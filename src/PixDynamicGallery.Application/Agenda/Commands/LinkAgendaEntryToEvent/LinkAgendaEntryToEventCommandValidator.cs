using FluentValidation;

namespace PixDynamicGallery.Application.Agenda.Commands.LinkAgendaEntryToEvent;

public class LinkAgendaEntryToEventCommandValidator : AbstractValidator<LinkAgendaEntryToEventCommand>
{
    public LinkAgendaEntryToEventCommandValidator()
    {
        RuleFor(x => x.AgendaEntryId).NotEmpty();
        RuleFor(x => x.EventId).NotEmpty();
    }
}
