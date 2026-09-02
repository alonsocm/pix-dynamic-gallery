using MediatR;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Application.Finance.Dtos;
using PixDynamicGallery.Domain.Entities;

namespace PixDynamicGallery.Application.Finance.Commands.AddGlobalExpense;

public class AddGlobalExpenseCommandHandler(IApplicationDbContext context)
    : IRequestHandler<AddGlobalExpenseCommand, GlobalExpenseDto>
{
    public async Task<GlobalExpenseDto> Handle(AddGlobalExpenseCommand request, CancellationToken cancellationToken)
    {
        var expense = GlobalExpense.Create(request.ExpenseDate, request.Category, request.Description, request.Amount);

        context.GlobalExpenses.Add(expense);
        await context.SaveChangesAsync(cancellationToken);

        return GlobalExpenseDto.FromEntity(expense);
    }
}
