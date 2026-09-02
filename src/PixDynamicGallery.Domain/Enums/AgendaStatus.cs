namespace PixDynamicGallery.Domain.Enums;

/// <summary>Lifecycle of a booking in the agenda, from first contact to a wrapped-up event.</summary>
public enum AgendaStatus
{
    /// <summary>A lead — date and client noted, nothing confirmed yet.</summary>
    Prospect = 0,

    /// <summary>Booked; expected to happen on <c>EventDate</c>.</summary>
    Confirmed = 1,

    /// <summary>The event happened.</summary>
    Completed = 2,

    /// <summary>Booking fell through.</summary>
    Cancelled = 3,
}
