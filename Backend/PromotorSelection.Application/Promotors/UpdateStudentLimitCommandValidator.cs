using FluentValidation;

namespace PromotorSelection.Application.Promotors;

public class UpdateStudentLimitCommandValidator : AbstractValidator<UpdateStudentLimitCommand>
{
    public UpdateStudentLimitCommandValidator()
    {
        RuleFor(x => x.NewLimit).GreaterThanOrEqualTo(0);
    }
}