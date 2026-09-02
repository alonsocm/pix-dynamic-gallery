using FluentValidation;

namespace PixDynamicGallery.Application.Agenda.Commands.DeleteAgendaEntry;

public class DeleteAgendaEntryCommandValidator : AbstractValidator<DeleteAgendaEntryCommand>
{
    public DeleteAgendaEntryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
