using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Common.Exceptions;
using PixDynamicGallery.Application.Common.Interfaces;

namespace PixDynamicGallery.Application.Finance.Commands.DeleteEventTransaction;

public class DeleteEventTransactionCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteEventTransactionCommand>
{
    public async Task Handle(DeleteEventTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = await context.EventTransactions.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.EventTransaction), request.Id);

        context.EventTransactions.Remove(transaction);
        await context.SaveChangesAsync(cancellationToken);
    }
}
