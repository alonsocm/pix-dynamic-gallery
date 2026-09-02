using FluentValidation;

namespace PixDynamicGallery.Application.Finance.Commands.AddUsbExpense;

public class AddUsbExpenseCommandValidator : AbstractValidator<AddUsbExpenseCommand>
{
    public AddUsbExpenseCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.UsbCount).GreaterThanOrEqualTo(0).When(x => x.UsbCount.HasValue);
        RuleFor(x => x.CostPerUsb).GreaterThanOrEqualTo(0).When(x => x.CostPerUsb.HasValue);
    }
}
