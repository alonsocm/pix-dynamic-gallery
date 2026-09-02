using MediatR;
using PixDynamicGallery.Application.Finance.Dtos;

namespace PixDynamicGallery.Application.Finance.Commands.UpdateFinanceSettings;

public record UpdateFinanceSettingsCommand : IRequest<FinanceSettingsDto>
{
    public required decimal CostPerKm { get; init; }
}
