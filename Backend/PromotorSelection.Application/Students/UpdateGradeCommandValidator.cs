using FluentValidation;

namespace PromotorSelection.Application.Students;

public class UpdateGradeCommandValidator : AbstractValidator<UpdateGradeCommand>
{
    public UpdateGradeCommandValidator()
    {
        RuleFor(x => x.NewGrade).InclusiveBetween(2.0, 5.5);
    }
}