using FluentValidation;

namespace PixDynamicGallery.Application.Inventory.Commands.DeleteUsbPurchase;

public class DeleteUsbPurchaseCommandValidator : AbstractValidator<DeleteUsbPurchaseCommand>
{
    public DeleteUsbPurchaseCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
