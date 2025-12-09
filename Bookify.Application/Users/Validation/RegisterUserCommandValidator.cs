using Bookify.Application.Users.Commands.RegisterUser;
using FluentValidation;

namespace Bookify.Application.Users.Validation;

internal sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(c => c.FirstName).NotEmpty();
        
        RuleFor(c => c.LastName).NotEmpty();
        
        RuleFor(c => c.Email).EmailAddress();
        
        RuleFor(c => c.Password).NotEmpty().MinimumLength(3);
    }
}