using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Common.Exceptions;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Application.Finance.Dtos;
using PixDynamicGallery.Application.Inventory;
using PixDynamicGallery.Domain.Entities;

namespace PixDynamicGallery.Application.Finance.Commands.AddUsbExpense;

public class AddUsbExpenseCommandHandler(IApplicationDbContext context)
    : IRequestHandler<AddUsbExpenseCommand, EventTransactionDto>
{
    public async Task<EventTransactionDto> Handle(AddUsbExpenseCommand request, CancellationToken cancellationToken)
    {
        var eventExists = await context.Events.AnyAsync(e => e.Id == request.EventId, cancellationToken);
        if (!eventExists)
        {
            throw new NotFoundException(nameof(Domain.Entities.Event), request.EventId);
        }

        var usbCount = request.UsbCount ?? 1;
        var costPerUsb = request.CostPerUsb
            ?? await UsbStockCalculator.GetSuggestedCostPerUsbAsync(context, cancellationToken);

        var transaction = EventTransaction.CreateUsbExpense(
            request.EventId, usbCount, costPerUsb, request.TransactionDate ?? DateTimeOffset.UtcNow);

        context.EventTransactions.Add(transaction);
        await context.SaveChangesAsync(cancellationToken);

        return EventTransactionDto.FromEntity(transaction);
    }
}
