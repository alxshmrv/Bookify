namespace Bookify.Application.Users.Dtos;

public sealed class UserResponseDto
{
    public Guid Id { get; init; }
    
    public string Email { get; init; }
    
    public string Name { get; init; }
}