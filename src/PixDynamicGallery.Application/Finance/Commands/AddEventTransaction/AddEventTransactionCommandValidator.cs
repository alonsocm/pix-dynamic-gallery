using FluentValidation;

namespace PixDynamicGallery.Application.Finance.Commands.AddEventTransaction;

public class AddEventTransactionCommandValidator : AbstractValidator<AddEventTransactionCommand>
{
    public AddEventTransactionCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}
