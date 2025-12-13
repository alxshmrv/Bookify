using Bookify.Application.Abstractions.Authentication;
using Bookify.Application.Abstractions.Messaging;
using Bookify.Application.Users.Dtos;
using Bookify.Domain.Abstractions;
using Bookify.Domain.Users.Validation;

namespace Bookify.Application.Users.Commands.LoginUser;

internal sealed class LogInUserCommandHandler : ICommandHandler<LogInUserCommand, AccessTokenResponseDto>
{
    private readonly IJwtService _jwtService;

    public LogInUserCommandHandler(IJwtService jwtService)
    {
        _jwtService = jwtService;
    }

    public async Task<Result<AccessTokenResponseDto>> Handle(
        LogInUserCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _jwtService.GetAccessTokenAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (result.IsFailure)
        {
            return Result.Failure<AccessTokenResponseDto>(UserErrors.InvalidCredentials);
        }
        
        return new AccessTokenResponseDto(result.Value);
    }
}