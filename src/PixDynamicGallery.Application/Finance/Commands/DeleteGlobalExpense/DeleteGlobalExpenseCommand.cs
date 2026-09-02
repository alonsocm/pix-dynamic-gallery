using MediatR;

namespace PixDynamicGallery.Application.Finance.Commands.DeleteGlobalExpense;

public record DeleteGlobalExpenseCommand : IRequest
{
    public required Guid Id { get; init; }
}
