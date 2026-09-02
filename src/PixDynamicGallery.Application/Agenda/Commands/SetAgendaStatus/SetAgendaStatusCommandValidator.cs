using FluentValidation;

namespace PixDynamicGallery.Application.Agenda.Commands.SetAgendaStatus;

public class SetAgendaStatusCommandValidator : AbstractValidator<SetAgendaStatusCommand>
{
    public SetAgendaStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
    }
}
