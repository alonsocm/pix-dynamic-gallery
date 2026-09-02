using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Common.Exceptions;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Application.Finance.Dtos;
using PixDynamicGallery.Domain.Entities;

namespace PixDynamicGallery.Application.Finance.Commands.AddGasolineExpense;

public class AddGasolineExpenseCommandHandler(IApplicationDbContext context)
    : IRequestHandler<AddGasolineExpenseCommand, EventTransactionDto>
{
    public async Task<EventTransactionDto> Handle(AddGasolineExpenseCommand request, CancellationToken cancellationToken)
    {
        var eventExists = await context.Events.AnyAsync(e => e.Id == request.EventId, cancellationToken);
        if (!eventExists)
        {
            throw new NotFoundException(nameof(Domain.Entities.Event), request.EventId);
        }

        var costPerKm = request.CostPerKm
            ?? (await context.FinanceSettings.FirstAsync(cancellationToken)).CostPerKm;

        var transaction = EventTransaction.CreateGasolineExpense(
            request.EventId, request.DistanceKm, costPerKm, request.TransactionDate ?? DateTimeOffset.UtcNow);

        context.EventTransactions.Add(transaction);
        await context.SaveChangesAsync(cancellationToken);

        return EventTransactionDto.FromEntity(transaction);
    }
}
