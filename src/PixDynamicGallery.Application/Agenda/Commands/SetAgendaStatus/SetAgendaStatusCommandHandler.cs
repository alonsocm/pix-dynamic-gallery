using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Agenda.Dtos;
using PixDynamicGallery.Application.Common.Exceptions;
using PixDynamicGallery.Application.Common.Interfaces;

namespace PixDynamicGallery.Application.Agenda.Commands.SetAgendaStatus;

public class SetAgendaStatusCommandHandler(IApplicationDbContext context)
    : IRequestHandler<SetAgendaStatusCommand, AgendaEntryDto>
{
    public async Task<AgendaEntryDto> Handle(SetAgendaStatusCommand request, CancellationToken cancellationToken)
    {
        var entry = await context.AgendaEntries.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.AgendaEntry), request.Id);

        entry.SetStatus(request.Status);
        await context.SaveChangesAsync(cancellationToken);

        return AgendaEntryDto.FromEntity(entry);
    }
}
