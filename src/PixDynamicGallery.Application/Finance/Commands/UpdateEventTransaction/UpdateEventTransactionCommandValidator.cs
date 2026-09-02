using FluentValidation;

namespace PixDynamicGallery.Application.Finance.Commands.UpdateEventTransaction;

public class UpdateEventTransactionCommandValidator : AbstractValidator<UpdateEventTransactionCommand>
{
    public UpdateEventTransactionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
