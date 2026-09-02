namespace PixDynamicGallery.Application.Finance.Dtos;

public record FinanceSettingsDto
{
    public required decimal CostPerKm { get; init; }
}
