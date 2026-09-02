using FluentValidation;

namespace PixDynamicGallery.Application.Inventory.Commands.AddUsbPurchase;

public class AddUsbPurchaseCommandValidator : AbstractValidator<AddUsbPurchaseCommand>
{
    public AddUsbPurchaseCommandValidator()
    {
        RuleFor(x => x.UnitsCount).GreaterThan(0);
        RuleFor(x => x.TotalCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
