using FluentValidation;

namespace PromotorSelection.Application.Students;

public class CreateStudentCommandValidator : AbstractValidator<CreateStudentCommand>
{
    public CreateStudentCommandValidator()
    {
        RuleFor(x => x.NrAlbumu).NotEmpty().Length(5, 10);
        RuleFor(x => x.SredniaOcen).InclusiveBetween(2.0, 5.0);
        RuleFor(x => x.UserId).GreaterThan(0);
    }
}