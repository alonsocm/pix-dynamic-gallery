using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Common.Exceptions;
using PixDynamicGallery.Application.Common.Interfaces;

namespace PixDynamicGallery.Application.Agenda.Commands.DeleteAgendaEntry;

public class DeleteAgendaEntryCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteAgendaEntryCommand>
{
    public async Task Handle(DeleteAgendaEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await context.AgendaEntries.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.AgendaEntry), request.Id);

        context.AgendaEntries.Remove(entry);
        await context.SaveChangesAsync(cancellationToken);
    }
}
