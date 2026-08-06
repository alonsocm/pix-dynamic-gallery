namespace PixDynamicGallery.Domain.Exceptions;

/// <summary>
/// Thrown when a domain invariant is violated. Distinct from validation errors (which are
/// caught before we ever reach the domain) — this represents "this should never happen if the
/// invariants hold" territory.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
