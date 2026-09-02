using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Application.Finance.Dtos;

namespace PixDynamicGallery.Application.Finance.Commands.UpdateFinanceSettings;

public class UpdateFinanceSettingsCommandHandler(IApplicationDbContext context)
    : IRequestHandler<UpdateFinanceSettingsCommand, FinanceSettingsDto>
{
    public async Task<FinanceSettingsDto> Handle(UpdateFinanceSettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = await context.FinanceSettings.FirstAsync(cancellationToken);

        settings.UpdateCostPerKm(request.CostPerKm);
        await context.SaveChangesAsync(cancellationToken);

        return new FinanceSettingsDto { CostPerKm = settings.CostPerKm };
    }
}
