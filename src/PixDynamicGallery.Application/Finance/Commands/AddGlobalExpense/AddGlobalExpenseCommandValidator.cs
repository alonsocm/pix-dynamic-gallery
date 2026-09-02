using FluentValidation;

namespace PixDynamicGallery.Application.Finance.Commands.AddGlobalExpense;

public class AddGlobalExpenseCommandValidator : AbstractValidator<AddGlobalExpenseCommand>
{
    public AddGlobalExpenseCommandValidator()
    {
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}
