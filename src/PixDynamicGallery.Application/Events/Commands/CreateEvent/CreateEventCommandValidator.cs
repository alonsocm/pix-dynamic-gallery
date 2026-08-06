using FluentValidation;

namespace PixDynamicGallery.Application.Events.Commands.CreateEvent;

public class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[a-z0-9]+(-[a-z0-9]+)*$")
            .WithMessage("Slug must be lowercase, URL-safe (letters, numbers and hyphens only), e.g. 'julia-and-mark-wedding'.");

        RuleFor(x => x.WatchFolderPath)
            .NotEmpty()
            .MaximumLength(1000);

        RuleFor(x => x.GuestBaseUrl)
            .NotEmpty()
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("GuestBaseUrl must be a valid absolute URL, e.g. 'https://gallery.mystudio.com'.");
    }
}
