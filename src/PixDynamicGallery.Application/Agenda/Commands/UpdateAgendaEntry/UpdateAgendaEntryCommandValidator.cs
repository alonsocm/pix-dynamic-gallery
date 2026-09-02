using FluentValidation;

namespace PixDynamicGallery.Application.Agenda.Commands.UpdateAgendaEntry;

public class UpdateAgendaEntryCommandValidator : AbstractValidator<UpdateAgendaEntryCommand>
{
    public UpdateAgendaEntryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ClientName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EventType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ContactPhone).MaximumLength(50);
        RuleFor(x => x.ContactEmail).MaximumLength(200).EmailAddress().When(x => !string.IsNullOrEmpty(x.ContactEmail));
        RuleFor(x => x.Location).MaximumLength(300);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.AgreedPrice).GreaterThanOrEqualTo(0).When(x => x.AgreedPrice.HasValue);
    }
}
