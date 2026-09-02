using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Common.Exceptions;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Application.Finance.Dtos;
using PixDynamicGallery.Domain.Entities;
using PixDynamicGallery.Domain.Enums;

namespace PixDynamicGallery.Application.Finance.Commands.AddPhotoExpense;

public class AddPhotoExpenseCommandHandler(IApplicationDbContext context)
    : IRequestHandler<AddPhotoExpenseCommand, EventTransactionDto>
{
    public async Task<EventTransactionDto> Handle(AddPhotoExpenseCommand request, CancellationToken cancellationToken)
    {
        var eventExists = await context.Events.AnyAsync(e => e.Id == request.EventId, cancellationToken);
        if (!eventExists)
        {
            throw new NotFoundException(nameof(Domain.Entities.Event), request.EventId);
        }

        var photoCount = request.PhotoCount
            ?? await context.Photos.CountAsync(p => p.EventId == request.EventId && p.Status == PhotoStatus.Uploaded, cancellationToken);

        var costPerPhoto = request.CostPerPhoto
            ?? await PaperStockCalculator.GetSuggestedCostPerPhotoAsync(context, cancellationToken);

        var transaction = EventTransaction.CreatePhotoExpense(
            request.EventId, photoCount, costPerPhoto, request.TransactionDate ?? DateTimeOffset.UtcNow);

        context.EventTransactions.Add(transaction);
        await context.SaveChangesAsync(cancellationToken);

        return EventTransactionDto.FromEntity(transaction);
    }
}
