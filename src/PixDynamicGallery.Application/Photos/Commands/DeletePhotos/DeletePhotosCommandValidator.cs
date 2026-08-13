using FluentValidation;

namespace PixDynamicGallery.Application.Photos.Commands.DeletePhotos;

public class DeletePhotosCommandValidator : AbstractValidator<DeletePhotosCommand>
{
    public DeletePhotosCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.PhotoIds).NotEmpty().WithMessage("Select at least one photo to delete.");
    }
}
