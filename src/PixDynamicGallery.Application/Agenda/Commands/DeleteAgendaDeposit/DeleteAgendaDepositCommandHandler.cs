using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Common.Exceptions;
using PixDynamicGallery.Application.Common.Interfaces;

namespace PixDynamicGallery.Application.Agenda.Commands.DeleteAgendaDeposit;

public class DeleteAgendaDepositCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteAgendaDepositCommand>
{
    public async Task Handle(DeleteAgendaDepositCommand request, CancellationToken cancellationToken)
    {
        var deposit = await context.AgendaDeposits.FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.AgendaDeposit), request.Id);

        if (deposit.TransferredTransactionId is not null)
        {
            // Already booked as real income on the linked Event — deleting here would silently
            // leave that EventTransaction orphaned with no matching deposit. Delete the
            // EventTransaction itself (from the event's finance screen) if it needs correcting.
            throw new Common.Exceptions.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(
                    nameof(request.Id),
                    "This deposit was already converted into an income transaction on the linked event — delete it from the event's finance screen instead."),
            ]);
        }

        context.AgendaDeposits.Remove(deposit);
        await context.SaveChangesAsync(cancellationToken);
    }
}
