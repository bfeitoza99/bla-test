using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bla.Api.Middleware;

/// <summary>
/// Catches unhandled exceptions and converts them into a safe RFC 7807
/// <see cref="ProblemDetails"/> 500 response. Never leaks exception messages, stack traces, or
/// database details to the client — those go to the logs only.
/// </summary>
/// <remarks>
/// Wired via <c>AddExceptionHandler</c> + <c>UseExceptionHandler</c>. Validation failures and
/// other expected 4xx outcomes are handled at the endpoint/validation layer, not here; this is the
/// last-resort net for genuinely unexpected faults.
/// </remarks>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Full detail to logs (server-side only), correlated by the request trace id.
        _logger.LogError(
            exception,
            "Unhandled exception while processing {Method} {Path}.",
            httpContext.Request.Method,
            httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        // Deliberately generic: no exception message, stack trace, or DB error surfaces here.
        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1",
                Detail = "The server encountered an unexpected error. Please try again later.",
            },
        });
    }
}
