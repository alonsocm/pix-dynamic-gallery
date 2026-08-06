namespace PixDynamicGallery.Domain.Common;

/// <summary>
/// Base type for every entity in the model. Identity is a server-generated <see cref="Guid"/>
/// so photos/events can be created safely offline (e.g. by the watcher) without round-tripping
/// to the database for an identity value first.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAtUtc { get; protected set; } = DateTimeOffset.UtcNow;

    // Equality by identity, as is standard for entities (as opposed to value objects).
    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity other || obj.GetType() != GetType())
        {
            return false;
        }

        return Id == other.Id;
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(BaseEntity? left, BaseEntity? right) => Equals(left, right);

    public static bool operator !=(BaseEntity? left, BaseEntity? right) => !Equals(left, right);
}
