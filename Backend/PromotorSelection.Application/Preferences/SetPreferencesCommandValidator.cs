using FluentValidation;

namespace PromotorSelection.Application.Preferences;

public class SetPreferencesCommandValidator : AbstractValidator<SetPreferencesCommand>
{
    public SetPreferencesCommandValidator()
    {
        RuleFor(x => x.PromotorIds).NotEmpty().Must(x => x.Count == 3); 
    }
}