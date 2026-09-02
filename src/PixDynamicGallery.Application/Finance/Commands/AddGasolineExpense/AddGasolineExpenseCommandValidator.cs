using FluentValidation;

namespace PixDynamicGallery.Application.Finance.Commands.AddGasolineExpense;

public class AddGasolineExpenseCommandValidator : AbstractValidator<AddGasolineExpenseCommand>
{
    public AddGasolineExpenseCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.DistanceKm).GreaterThan(0);
        RuleFor(x => x.CostPerKm).GreaterThanOrEqualTo(0).When(x => x.CostPerKm.HasValue);
    }
}
