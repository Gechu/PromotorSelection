using FluentValidation;
using PromotorSelection.Application.Users;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.GradeAverage).InclusiveBetween(2.0, 5.0).When(x => x.GradeAverage.HasValue);
        RuleFor(x => x.StudentLimit).GreaterThan(0).When(x => x.StudentLimit.HasValue);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.AlbumNumber).NotEmpty();
    }
}