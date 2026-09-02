using FluentValidation;

namespace PixDynamicGallery.Application.Finance.Commands.DeleteGlobalExpense;

public class DeleteGlobalExpenseCommandValidator : AbstractValidator<DeleteGlobalExpenseCommand>
{
    public DeleteGlobalExpenseCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
