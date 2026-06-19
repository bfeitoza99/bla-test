using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bla.Application.Users;
using FluentAssertions;

namespace Bla.Api.Tests.Auth;

/// <summary>
/// End-to-end endpoint tests over the real HTTP pipeline (in-memory repository): the register ->
/// login -> /me happy path, plus the security-relevant edge cases (duplicate email -> 409,
/// bad credentials -> generic 401, unauthenticated /me -> 401, invalid input -> 400).
/// </summary>
public class AuthEndpointsTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public AuthEndpointsTests(AuthApiFactory factory) => _factory = factory;

    private static RegisterRequest NewAccount() =>
        new($"user-{Guid.NewGuid():N}@bla.local", "Password1");

    [Fact]
    public async Task Register_Login_Me_HappyPath()
    {
        var client = _factory.CreateClient();
        var account = NewAccount();

        // Register -> 201 with a token.
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", account);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var registered = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        registered.Should().NotBeNull();
        registered!.Token.Should().NotBeNullOrWhiteSpace();

        // Login -> 200 with a token.
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(account.Email, account.Password));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loggedIn = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        loggedIn.Should().NotBeNull();
        loggedIn!.Token.Should().NotBeNullOrWhiteSpace();

        // /me with the bearer token -> 200 with the user, email echoed back normalized.
        var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loggedIn.Token);
        var meResponse = await client.SendAsync(meRequest);
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var me = await meResponse.Content.ReadFromJsonAsync<UserResponse>();
        me.Should().NotBeNull();
        me!.Email.Should().Be(account.Email.ToLowerInvariant());
        me.Id.Should().NotBe(Guid.Empty);

        // The response body must never carry a password hash.
        var rawMe = await meResponse.Content.ReadAsStringAsync();
        rawMe.ToLowerInvariant().Should().NotContain("hash");
        rawMe.ToLowerInvariant().Should().NotContain("password");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var client = _factory.CreateClient();
        var account = NewAccount();

        (await client.PostAsJsonAsync("/api/auth/register", account)).EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/api/auth/register", account);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithWeakPassword_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("weak@bla.local", "weak"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401_Generic()
    {
        var client = _factory.CreateClient();
        var account = NewAccount();
        (await client.PostAsJsonAsync("/api/auth/register", account)).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(account.Email, "WrongPass1"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = (await response.Content.ReadAsStringAsync()).ToLowerInvariant();
        // Generic message — must not reveal which field was wrong.
        body.Should().NotContain("password is incorrect");
        body.Should().NotContain("unknown email");
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest($"ghost-{Guid.NewGuid():N}@bla.local", "Password1"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task OpenApiDocument_IncludesAuthEndpoints()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("\"openapi\": \"3.0");
        body.Should().Contain("/api/auth/register");
        body.Should().Contain("/api/auth/login");
        body.Should().Contain("/api/auth/me");
    }
}
