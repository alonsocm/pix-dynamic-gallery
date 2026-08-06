using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Domain.Entities;

namespace PixDynamicGallery.Application.Common.Interfaces;

/// <summary>
/// Application's view of the persistence layer. Handlers depend on this abstraction rather than
/// the concrete EF Core <c>DbContext</c>, which lives in Infrastructure — keeps Application free
/// of any dependency on EF Core's implementation details (or on Infrastructure at all).
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Event> Events { get; }

    DbSet<Photo> Photos { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
