using Bla.Application.Users;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Bla.Application;

/// <summary>
/// Composition seam for the Application layer.
/// Feature agents register their use-case services, request validators, and any
/// Application-level options here. Intentionally empty for the foundation — the
/// extension point exists so feature work does not have to touch Program.cs wiring order.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Application-layer services. Call once from the API composition root.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Auth slice: use-case service + request validators.
        services.AddScoped<IAuthService, AuthService>();
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

        return services;
    }
}
