using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Application.Events.Dtos;
using PixDynamicGallery.Domain.Entities;

namespace PixDynamicGallery.Application.Events.Commands.CreateEvent;

public class CreateEventCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateEventCommand, EventDto>
{
    public async Task<EventDto> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();

        var slugTaken = await context.Events.AnyAsync(e => e.Slug == slug, cancellationToken);
        if (slugTaken)
        {
            throw new Common.Exceptions.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(request.Slug), $"Slug '{slug}' is already in use by another event."),
            ]);
        }

        var @event = Event.Create(request.Name, slug, request.WatchFolderPath, request.GuestBaseUrl);

        context.Events.Add(@event);
        await context.SaveChangesAsync(cancellationToken);

        return EventDto.FromEntity(@event);
    }
}
