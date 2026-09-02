using MediatR;
using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Application.Common.Interfaces;
using PixDynamicGallery.Application.Finance.Dtos;

namespace PixDynamicGallery.Application.Finance.Queries.GetFinanceSettings;

public record GetFinanceSettingsQuery : IRequest<FinanceSettingsDto>;

public class GetFinanceSettingsQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetFinanceSettingsQuery, FinanceSettingsDto>
{
    public async Task<FinanceSettingsDto> Handle(GetFinanceSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await context.FinanceSettings.FirstAsync(cancellationToken);
        return new FinanceSettingsDto { CostPerKm = settings.CostPerKm };
    }
}
