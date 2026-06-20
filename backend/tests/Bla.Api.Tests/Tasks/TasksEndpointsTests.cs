using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bla.Application.Common;
using Bla.Application.Tasks;
using FluentAssertions;
using TaskStatus = Bla.Domain.Tasks.TaskStatus;

namespace Bla.Api.Tests.Tasks;

/// <summary>
/// End-to-end endpoint tests over the real HTTP pipeline (in-memory repository, real JWT): the
/// create -> list(paginated) -> get -> update -> delete happy path, plus the security-relevant edge
/// cases (no token -> 401, another user's task -> 404, invalid input -> 400).
/// </summary>
public class TasksEndpointsTests : IClassFixture<TasksApiFactory>
{
    private readonly TasksApiFactory _factory;

    public TasksEndpointsTests(TasksApiFactory factory) => _factory = factory;

    // The API serializes enums as strings ("Todo"/"Done"); deserialize responses the same way.
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static CreateTaskRequest NewTask(string title = "Sample") =>
        new(title, "A description", TaskStatus.Todo, DateTime.UtcNow.AddDays(3));

    [Fact]
    public async Task Create_List_Get_Update_Delete_HappyPath()
    {
        var client = _factory.CreateAuthenticatedClient(out _);

        // Create -> 201 with Location and the new task.
        var createResponse = await client.PostAsJsonAsync("/api/tasks", NewTask("First task"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        createResponse.Headers.Location.Should().NotBeNull();
        var created = await createResponse.Content.ReadFromJsonAsync<TaskResponse>(JsonOpts);
        created.Should().NotBeNull();
        created!.Title.Should().Be("First task");
        created.Id.Should().NotBe(Guid.Empty);

        // List -> 200 paginated, with the created task present and the paging shape correct.
        var listResponse = await client.GetAsync("/api/tasks?page=1&pageSize=10");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResult<TaskResponse>>(JsonOpts);
        page.Should().NotBeNull();
        page!.Page.Should().Be(1);
        page.PageSize.Should().Be(10);
        page.Total.Should().BeGreaterThanOrEqualTo(1);
        page.Items.Should().Contain(t => t.Id == created.Id);

        // Get -> 200 with the same task.
        var getResponse = await client.GetAsync($"/api/tasks/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<TaskResponse>(JsonOpts);
        fetched!.Id.Should().Be(created.Id);

        // Update -> 200 with the new state.
        var update = new UpdateTaskRequest("Renamed", "Updated", TaskStatus.Done, DateTime.UtcNow.AddDays(5));
        var updateResponse = await client.PutAsJsonAsync($"/api/tasks/{created.Id}", update);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<TaskResponse>(JsonOpts);
        updated!.Title.Should().Be("Renamed");
        updated.Status.Should().Be(TaskStatus.Done);
        updated.UpdatedAt.Should().BeOnOrAfter(created.UpdatedAt);

        // Delete -> 204, then Get -> 404.
        var deleteResponse = await client.DeleteAsync($"/api/tasks/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterDelete = await client.GetAsync($"/api/tasks/{created.Id}");
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_FiltersByStatus()
    {
        var client = _factory.CreateAuthenticatedClient(out _);

        await client.PostAsJsonAsync("/api/tasks",
            new CreateTaskRequest("Todo one", null, TaskStatus.Todo, DateTime.UtcNow.AddDays(1)));
        var doneResponse = await client.PostAsJsonAsync("/api/tasks",
            new CreateTaskRequest("Done one", null, TaskStatus.Done, DateTime.UtcNow.AddDays(2)));
        var doneTask = await doneResponse.Content.ReadFromJsonAsync<TaskResponse>(JsonOpts);

        var listResponse = await client.GetAsync("/api/tasks?status=Done");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResult<TaskResponse>>(JsonOpts);

        page!.Items.Should().OnlyContain(t => t.Status == TaskStatus.Done);
        page.Items.Should().Contain(t => t.Id == doneTask!.Id);
    }

    [Fact]
    public async Task Create_WithInvalidInput_Returns400()
    {
        var client = _factory.CreateAuthenticatedClient(out _);

        var response = await client.PostAsJsonAsync("/api/tasks",
            new CreateTaskRequest("", null, TaskStatus.Todo, DateTime.UtcNow.AddDays(1)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_WithBadPaging_Returns400()
    {
        var client = _factory.CreateAuthenticatedClient(out _);

        var response = await client.GetAsync("/api/tasks?page=0&pageSize=9999");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Tasks_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/tasks");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_AnotherUsersTask_Returns404()
    {
        // User A creates a task.
        var clientA = _factory.CreateAuthenticatedClient(out _);
        var createResponse = await clientA.PostAsJsonAsync("/api/tasks", NewTask("A's secret"));
        var created = await createResponse.Content.ReadFromJsonAsync<TaskResponse>(JsonOpts);

        // User B must not be able to see it — 404, not 403, so existence isn't leaked.
        var clientB = _factory.CreateAuthenticatedClient(out _);
        var crossGet = await clientB.GetAsync($"/api/tasks/{created!.Id}");
        crossGet.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // …and cannot update or delete it either.
        var crossUpdate = await clientB.PutAsJsonAsync($"/api/tasks/{created.Id}",
            new UpdateTaskRequest("Hijack", null, TaskStatus.Done, DateTime.UtcNow.AddDays(1)));
        crossUpdate.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var crossDelete = await clientB.DeleteAsync($"/api/tasks/{created.Id}");
        crossDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task OpenApiDocument_IncludesTaskEndpointsAndPagedShape()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("\"openapi\": \"3.0");
        body.Should().Contain("/api/tasks");
        // The paged response shape is part of the contract the Angular client generates from.
        body.Should().Contain("PagedResultOfTaskResponse");
    }
}
