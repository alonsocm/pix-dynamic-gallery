using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Common.Exceptions;
using PixDynamicGallery.Application.Common.Interfaces;

namespace PixDynamicGallery.Application.Finance.Commands.DeleteGlobalExpense;

public class DeleteGlobalExpenseCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteGlobalExpenseCommand>
{
    public async Task Handle(DeleteGlobalExpenseCommand request, CancellationToken cancellationToken)
    {
        var expense = await context.GlobalExpenses.FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.GlobalExpense), request.Id);

        context.GlobalExpenses.Remove(expense);
        await context.SaveChangesAsync(cancellationToken);
    }
}
