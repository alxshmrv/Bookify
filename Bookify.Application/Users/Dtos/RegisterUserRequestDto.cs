namespace Bookify.Application.Users.Dtos;

public sealed record RegisterUserRequestDto(
    string Email,
    string FirstName,
    string LastName,
    string Password);
