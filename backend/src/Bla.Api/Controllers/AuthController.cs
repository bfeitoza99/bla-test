using Bla.Api.Authentication;
using Bla.Application.Users;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bla.Api.Controllers;

/// <summary>
/// Users/Auth API: public register and login, plus an authorized "current user" endpoint.
/// The controller is thin — it delegates to <see cref="IAuthService"/> and translates the
/// outcome (or a known use-case exception) into the right HTTP status + ProblemDetails.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Registers a new account and returns an access token.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.RegisterAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(ex);
        }
        catch (EmailAlreadyInUseException)
        {
            return Problem(
                title: "Email already in use.",
                detail: "An account with this email already exists.",
                statusCode: StatusCodes.Status409Conflict);
        }
    }

    /// <summary>Authenticates and returns an access token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.LoginAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(ex);
        }
        catch (InvalidCredentialsException)
        {
            // Generic by design: never reveal whether the email or the password was wrong.
            return Problem(
                title: "Invalid credentials.",
                detail: "Invalid email or password.",
                statusCode: StatusCodes.Status401Unauthorized);
        }
    }

    /// <summary>Returns the authenticated user's profile.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var response = await _authService.GetCurrentUserAsync(userId, cancellationToken);
        if (response is null)
        {
            return Problem(
                title: "User not found.",
                detail: "The authenticated user no longer exists.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(response);
    }

    private ActionResult ValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return ValidationProblem(new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
        });
    }
}
