namespace PixDynamicGallery.Domain.Enums;

/// <summary>
/// Classifies an <see cref="Entities.EventTransaction"/> or <see cref="Entities.GlobalExpense"/>.
/// Income and expense categories share one enum for simplicity; which ones make sense together
/// with which <see cref="FinanceTransactionType"/> is enforced by validators, not the type system.
/// </summary>
public enum FinanceCategory
{
    /// <summary>Income: a payment/deposit from the client.</summary>
    Payment = 0,

    /// <summary>Income: a tip.</summary>
    Tip = 1,

    /// <summary>Expense: printed photos — see <see cref="Entities.EventTransaction.CreatePhotoExpense"/>.</summary>
    Photos = 2,

    /// <summary>Expense: fuel to/from the event — see <see cref="Entities.EventTransaction.CreateGasolineExpense"/>.</summary>
    Gasoline = 3,

    /// <summary>Expense: paper/ink stock. Actual purchases are tracked as <see cref="Entities.PaperPurchase"/>; this category is for ad-hoc paper-related event expenses.</summary>
    Paper = 4,

    /// <summary>Expense: equipment (booth hardware, props, etc.).</summary>
    Equipment = 5,

    /// <summary>Expense: marketing/advertising.</summary>
    Marketing = 6,

    /// <summary>Anything that doesn't fit another category, income or expense.</summary>
    Other = 7,

    /// <summary>Expense: the USB drive handed to the client at each event — see <see cref="Entities.EventTransaction.CreateUsbExpense"/>.</summary>
    Usb = 8,
}
