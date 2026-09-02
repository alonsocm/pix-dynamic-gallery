using FluentValidation;

namespace PixDynamicGallery.Application.Inventory.Commands.DeletePaperPurchase;

public class DeletePaperPurchaseCommandValidator : AbstractValidator<DeletePaperPurchaseCommand>
{
    public DeletePaperPurchaseCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
