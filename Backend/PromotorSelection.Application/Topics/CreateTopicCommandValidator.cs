using FluentValidation;

namespace PromotorSelection.Application.Topics;

public class CreateTopicCommandValidator : AbstractValidator<CreateTopicCommand>
{
    public CreateTopicCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(50);

        RuleFor(x => x.Description).MaximumLength(200);
    }
}