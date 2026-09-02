using FluentValidation;

namespace PixDynamicGallery.Application.Finance.Commands.UpdateFinanceSettings;

public class UpdateFinanceSettingsCommandValidator : AbstractValidator<UpdateFinanceSettingsCommand>
{
    public UpdateFinanceSettingsCommandValidator()
    {
        RuleFor(x => x.CostPerKm).GreaterThanOrEqualTo(0);
    }
}
