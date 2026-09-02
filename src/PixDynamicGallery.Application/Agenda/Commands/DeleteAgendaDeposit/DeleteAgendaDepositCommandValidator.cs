using FluentValidation;

namespace PixDynamicGallery.Application.Agenda.Commands.DeleteAgendaDeposit;

public class DeleteAgendaDepositCommandValidator : AbstractValidator<DeleteAgendaDepositCommand>
{
    public DeleteAgendaDepositCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
