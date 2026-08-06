using FluentValidation;

namespace PixDynamicGallery.Application.Photos.Commands.UploadCapturedPhoto;

public class UploadCapturedPhotoCommandValidator : AbstractValidator<UploadCapturedPhotoCommand>
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif"];

    public UploadCapturedPhotoCommandValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty();

        RuleFor(x => x.LocalFilePath)
            .NotEmpty()
            .Must(path => AllowedExtensions.Contains(Path.GetExtension(path).ToLowerInvariant()))
            .WithMessage($"Only {string.Join(", ", AllowedExtensions)} files captured by Sparkbooth are supported.");
    }
}
