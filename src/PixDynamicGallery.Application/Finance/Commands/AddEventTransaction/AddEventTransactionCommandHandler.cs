using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Common.Exceptions;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Application.Finance.Dtos;
using PixDynamicGallery.Domain.Entities;

namespace PixDynamicGallery.Application.Finance.Commands.AddEventTransaction;

public class AddEventTransactionCommandHandler(IApplicationDbContext context)
    : IRequestHandler<AddEventTransactionCommand, EventTransactionDto>
{
    public async Task<EventTransactionDto> Handle(AddEventTransactionCommand request, CancellationToken cancellationToken)
    {
        var eventExists = await context.Events.AnyAsync(e => e.Id == request.EventId, cancellationToken);
        if (!eventExists)
        {
            throw new NotFoundException(nameof(Domain.Entities.Event), request.EventId);
        }

        var transaction = EventTransaction.CreateManual(
            request.EventId, request.Type, request.Category, request.Description, request.Amount, request.TransactionDate);

        context.EventTransactions.Add(transaction);
        await context.SaveChangesAsync(cancellationToken);

        return EventTransactionDto.FromEntity(transaction);
    }
}
