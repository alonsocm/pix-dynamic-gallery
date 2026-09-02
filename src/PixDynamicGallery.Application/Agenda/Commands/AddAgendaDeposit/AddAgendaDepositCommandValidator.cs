using FluentValidation;

namespace PixDynamicGallery.Application.Agenda.Commands.AddAgendaDeposit;

public class AddAgendaDepositCommandValidator : AbstractValidator<AddAgendaDepositCommand>
{
    public AddAgendaDepositCommandValidator()
    {
        RuleFor(x => x.AgendaEntryId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
