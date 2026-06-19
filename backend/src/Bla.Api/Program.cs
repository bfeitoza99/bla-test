using System.Text;
using Bla.Api.Authentication;
using Bla.Api.Health;
using Bla.Api.Middleware;
using Bla.Application;
using Bla.Infrastructure;
using Bla.Infrastructure.Migrations;
using Bla.Infrastructure.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Npgsql;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Configuration-driven cross-cutting values
// ---------------------------------------------------------------------------
var configuration = builder.Configuration;

var connectionString =
    configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Missing required configuration 'ConnectionStrings:Default'.");

var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Missing required configuration section 'Jwt'.");

if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey)
    || string.IsNullOrWhiteSpace(jwtOptions.Issuer)
    || string.IsNullOrWhiteSpace(jwtOptions.Audience))
{
    throw new InvalidOperationException(
        "'Jwt' configuration requires non-empty Issuer, Audience, and SigningKey.");
}

var corsAllowedOrigin =
    configuration["Cors:AllowedOrigin"]
    ?? throw new InvalidOperationException(
        "Missing required configuration 'Cors:AllowedOrigin'.");

const string CorsPolicyName = "SpaCors";

// ---------------------------------------------------------------------------
// Data source (single shared NpgsqlDataSource for repos + health check)
// ---------------------------------------------------------------------------
builder.Services.AddNpgsqlDataSource(connectionString);

// ---------------------------------------------------------------------------
// Cross-cutting framework services
// ---------------------------------------------------------------------------

// RFC 7807 ProblemDetails for every error response shape.
builder.Services.AddProblemDetails();

// Last-resort exception net -> generic 500 ProblemDetails (no stack/DB leak).
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Native OpenAPI document, pinned to OpenAPI 3.0 for broad client/generator compatibility.
builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
});

// CORS: a specific SPA origin only (never AllowAnyOrigin); bearer tokens, so no credentials.
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
        policy
            .WithOrigins(corsAllowedOrigin)
            .WithHeaders("Authorization", "Content-Type")
            .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS"));
});

// JWT bearer authentication + authorization.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization();

// Health checks: DB readiness. The check tolerates an unavailable DB (degrades, never throws).
builder.Services.AddHealthChecks()
    .AddCheck<NpgsqlHealthCheck>(
        "database",
        tags: ["ready"]);

// Controllers are added so feature agents can drop in their endpoints without touching wiring.
builder.Services.AddControllers();

// ---------------------------------------------------------------------------
// Application + Infrastructure composition seams (feature agents extend these)
// ---------------------------------------------------------------------------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(configuration);

var app = builder.Build();

// ---------------------------------------------------------------------------
// Run pending SQL migrations on startup. Tolerate an unavailable DB at boot:
// log and continue so the API (and its public endpoints) still come up.
// ---------------------------------------------------------------------------
await ApplyMigrationsAsync(app);

// Seed the demo account (idempotent). Tolerate an unavailable DB at boot, like migrations.
await SeedDemoUserAsync(app);

// ---------------------------------------------------------------------------
// HTTP pipeline
// ---------------------------------------------------------------------------
app.UseExceptionHandler();

// OpenAPI document at /openapi/v1.json and the Scalar reference UI at /scalar.
app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();

app.UseCors(CorsPolicyName);

app.UseAuthentication();
app.UseAuthorization();

// Public health endpoint (no auth) — also demonstrates the non-authorized path.
app.MapHealthChecks("/health").AllowAnonymous();

app.MapControllers();

app.Run();

// ---------------------------------------------------------------------------
// Local helpers
// ---------------------------------------------------------------------------
static async Task ApplyMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger("Startup.Migrations");
    try
    {
        var runner = scope.ServiceProvider.GetRequiredService<MigrationRunner>();
        await runner.MigrateAsync();
    }
    catch (Exception ex)
    {
        // Don't crash the host if the DB isn't reachable at boot; the health check will report it.
        logger.LogError(ex, "Database migrations could not be applied at startup; continuing.");
    }
}

static async Task SeedDemoUserAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger("Startup.Seed");
    try
    {
        var seeder = scope.ServiceProvider.GetRequiredService<DemoUserSeeder>();
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        // As with migrations: a DB that's unreachable at boot must not crash the host.
        logger.LogError(ex, "Demo user could not be seeded at startup; continuing.");
    }
}

/// <summary>
/// Exposed so the API integration tests can spin the app up via <c>WebApplicationFactory</c>.
/// </summary>
public partial class Program;
