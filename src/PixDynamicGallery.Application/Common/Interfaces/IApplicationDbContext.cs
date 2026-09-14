using Microsoft.EntityFrameworkCore;
using PixDynamicGallery.Domain.Entities;

namespace PixDynamicGallery.Application.Common.Interfaces;

/// <summary>
/// Application's view of the persistence layer. Handlers depend on this abstraction rather than
/// the concrete EF Core <c>DbContext</c>, which lives in Infrastructure — keeps Application free
/// of any dependency on EF Core's implementation details (or on Infrastructure at all).
///
/// Only <see cref="Event"/>/<see cref="Photo"/> are mapped here — that's all the cabin's watcher
/// pipeline touches. Agenda/Finance/Inventory/EventTransaction moved entirely to the `pix-app`
/// Cloudflare Worker (separate repo); Neon still has those tables, `pix-app` owns them now.
/// IMPORTANT: never run `dotnet ef migrations add` without knowing this — since this model no
/// longer describes those tables, a new migration would generate DROP TABLE statements against
/// data `pix-app` is actively using.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Event> Events { get; }

    DbSet<Photo> Photos { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
