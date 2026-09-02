using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Agenda.Dtos;
using PixDynamicGallery.Application.Common.Exceptions;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Domain.Entities;
using PixDynamicGallery.Domain.Enums;

namespace PixDynamicGallery.Application.Agenda.Commands.AddAgendaDeposit;

public class AddAgendaDepositCommandHandler(IApplicationDbContext context)
    : IRequestHandler<AddAgendaDepositCommand, AgendaDepositDto>
{
    public async Task<AgendaDepositDto> Handle(AddAgendaDepositCommand request, CancellationToken cancellationToken)
    {
        var entry = await context.AgendaEntries.FirstOrDefaultAsync(a => a.Id == request.AgendaEntryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.AgendaEntry), request.AgendaEntryId);

        var deposit = AgendaDeposit.Create(request.AgendaEntryId, request.Amount, request.PaymentDate, request.Notes);
        context.AgendaDeposits.Add(deposit);

        // If this booking already turned into a technical Event, don't make the operator re-link
        // just to get the deposit counted — book the income straight away (LinkAgendaEntryToEvent
        // only sweeps deposits that existed at the moment of linking).
        if (entry.LinkedEventId is Guid eventId)
        {
            var description = string.IsNullOrWhiteSpace(deposit.Notes)
                ? "Anticipo de agenda"
                : $"Anticipo de agenda — {deposit.Notes}";

            var transaction = EventTransaction.CreateManual(
                eventId, FinanceTransactionType.Income, FinanceCategory.Payment, description, deposit.Amount, deposit.PaymentDate);

            context.EventTransactions.Add(transaction);
            deposit.MarkTransferred(transaction.Id);
        }

        await context.SaveChangesAsync(cancellationToken);

        return AgendaDepositDto.FromEntity(deposit);
    }
}
