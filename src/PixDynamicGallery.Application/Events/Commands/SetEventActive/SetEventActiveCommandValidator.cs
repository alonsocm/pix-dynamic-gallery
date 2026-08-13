using FluentValidation;

namespace PixDynamicGallery.Application.Events.Commands.SetEventActive;

public class SetEventActiveCommandValidator : AbstractValidator<SetEventActiveCommand>
{
    public SetEventActiveCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
    }
}
