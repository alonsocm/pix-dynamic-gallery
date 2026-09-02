using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Agenda.Dtos;
using PixDynamicGallery.Application.Common.Exceptions;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Domain.Entities;
using PixDynamicGallery.Domain.Enums;

namespace PixDynamicGallery.Application.Agenda.Commands.LinkAgendaEntryToEvent;

public class LinkAgendaEntryToEventCommandHandler(IApplicationDbContext context)
    : IRequestHandler<LinkAgendaEntryToEventCommand, AgendaEntryDto>
{
    public async Task<AgendaEntryDto> Handle(LinkAgendaEntryToEventCommand request, CancellationToken cancellationToken)
    {
        var entry = await context.AgendaEntries.FirstOrDefaultAsync(a => a.Id == request.AgendaEntryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.AgendaEntry), request.AgendaEntryId);

        var eventExists = await context.Events.AnyAsync(e => e.Id == request.EventId, cancellationToken);
        if (!eventExists)
        {
            throw new NotFoundException(nameof(Domain.Entities.Event), request.EventId);
        }

        entry.LinkToEvent(request.EventId);

        // Every deposit taken while this was still just an agenda booking becomes real income on
        // the event it turned into — so the event's own P&L (and the global dashboard) reflects
        // money that was, in fact, already collected. Guarded by TransferredTransactionId so
        // linking twice (or re-linking) never double-books a deposit as income.
        var untransferredDeposits = await context.AgendaDeposits
            .Where(d => d.AgendaEntryId == entry.Id && d.TransferredTransactionId == null)
            .ToListAsync(cancellationToken);

        foreach (var deposit in untransferredDeposits)
        {
            var description = string.IsNullOrWhiteSpace(deposit.Notes)
                ? "Anticipo de agenda"
                : $"Anticipo de agenda — {deposit.Notes}";

            var transaction = EventTransaction.CreateManual(
                request.EventId, FinanceTransactionType.Income, FinanceCategory.Payment, description, deposit.Amount, deposit.PaymentDate);

            context.EventTransactions.Add(transaction);
            deposit.MarkTransferred(transaction.Id);
        }

        await context.SaveChangesAsync(cancellationToken);

        var deposits = await context.AgendaDeposits
            .Where(d => d.AgendaEntryId == entry.Id)
            .OrderByDescending(d => d.PaymentDate)
            .ToListAsync(cancellationToken);

        return AgendaEntryDto.FromEntity(entry, deposits);
    }
}
