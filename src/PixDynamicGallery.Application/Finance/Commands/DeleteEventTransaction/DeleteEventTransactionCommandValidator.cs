using FluentValidation;

namespace PixDynamicGallery.Application.Finance.Commands.DeleteEventTransaction;

public class DeleteEventTransactionCommandValidator : AbstractValidator<DeleteEventTransactionCommand>
{
    public DeleteEventTransactionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
