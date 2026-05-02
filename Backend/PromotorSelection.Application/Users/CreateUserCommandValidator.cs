using FluentValidation;

namespace PromotorSelection.Application.Users;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.RoleId).InclusiveBetween(1, 3);
        RuleFor(x => x.AlbumNumber).NotEmpty().When(x => x.RoleId == 1);
        RuleFor(x => x.StudentLimit).NotEmpty().GreaterThan(0).When(x => x.RoleId == 2);
    }
}