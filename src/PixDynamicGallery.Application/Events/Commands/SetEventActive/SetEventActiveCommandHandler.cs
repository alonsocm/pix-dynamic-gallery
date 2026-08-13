using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Common.Exceptions;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Application.Events.Dtos;

namespace PixDynamicGallery.Application.Events.Commands.SetEventActive;

public class SetEventActiveCommandHandler(IApplicationDbContext context) : IRequestHandler<SetEventActiveCommand, EventDto>
{
    public async Task<EventDto> Handle(SetEventActiveCommand request, CancellationToken cancellationToken)
    {
        var @event = await context.Events
            .Include(e => e.Photos)
            .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Event), request.EventId);

        if (request.IsActive)
        {
            @event.Activate();
        }
        else
        {
            @event.Deactivate();
        }

        await context.SaveChangesAsync(cancellationToken);

        return EventDto.FromEntity(@event);
    }
}
