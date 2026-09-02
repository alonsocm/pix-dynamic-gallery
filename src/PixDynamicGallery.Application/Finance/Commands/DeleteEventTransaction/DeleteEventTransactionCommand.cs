using MediatR;

namespace PixDynamicGallery.Application.Finance.Commands.DeleteEventTransaction;

public record DeleteEventTransactionCommand : IRequest
{
    public required Guid Id { get; init; }
}
