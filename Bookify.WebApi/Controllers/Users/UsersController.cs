using Bookify.Application.Users.Commands.LoginUser;
using Bookify.Application.Users.Commands.RegisterUser;
using Bookify.Application.Users.Dtos;
using Bookify.Application.Users.Queries.GetLoggedInUser;
using Bookify.WebApi.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookify.WebApi.Controllers.Users;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("me")]
    [Authorize(Roles = Roles.Registered)]
    public async Task<IActionResult> GetLoggedInUser(CancellationToken cancellationToken)
    {
        var query = new GetLoggedInUserQuery();
        
        var result = await _sender.Send(query, cancellationToken);
        
        return Ok(result.Value);
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(
            request.Email,
            request.FirstName,
            request.LastName,
            request.Password);
        
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    [HttpPost("login")]
    public async Task<IActionResult> LogIn(
        LogInUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new LogInUserCommand(request.Email, request.Password);
        
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return Unauthorized(result.Error);
        }
        
        return Ok(result.Value);
    }
}