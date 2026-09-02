using FluentValidation;

namespace PixDynamicGallery.Application.Finance.Commands.AddPaperPurchase;

public class AddPaperPurchaseCommandValidator : AbstractValidator<AddPaperPurchaseCommand>
{
    public AddPaperPurchaseCommandValidator()
    {
        RuleFor(x => x.SheetsCount).GreaterThan(0);
        RuleFor(x => x.TotalCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
