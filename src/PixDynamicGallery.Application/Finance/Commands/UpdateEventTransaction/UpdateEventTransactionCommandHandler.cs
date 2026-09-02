using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Common.Exceptions;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Application.Finance.Dtos;

namespace PixDynamicGallery.Application.Finance.Commands.UpdateEventTransaction;

public class UpdateEventTransactionCommandHandler(IApplicationDbContext context)
    : IRequestHandler<UpdateEventTransactionCommand, EventTransactionDto>
{
    public async Task<EventTransactionDto> Handle(UpdateEventTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = await context.EventTransactions.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.EventTransaction), request.Id);

        transaction.Update(request.Amount, request.Description, request.TransactionDate);
        await context.SaveChangesAsync(cancellationToken);

        return EventTransactionDto.FromEntity(transaction);
    }
}
