using FluentValidation;

namespace PixDynamicGallery.Application.Finance.Commands.DeletePaperPurchase;

public class DeletePaperPurchaseCommandValidator : AbstractValidator<DeletePaperPurchaseCommand>
{
    public DeletePaperPurchaseCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
