using FluentValidation;

namespace PixDynamicGallery.Application.Finance.Commands.AddPhotoExpense;

public class AddPhotoExpenseCommandValidator : AbstractValidator<AddPhotoExpenseCommand>
{
    public AddPhotoExpenseCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.PhotoCount).GreaterThanOrEqualTo(0).When(x => x.PhotoCount.HasValue);
        RuleFor(x => x.CostPerPhoto).GreaterThanOrEqualTo(0).When(x => x.CostPerPhoto.HasValue);
    }
}
