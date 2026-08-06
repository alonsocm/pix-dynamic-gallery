using Microsoft.EntityFrameworkCore;

namespace PixDynamicGallery.Application.Common.Models;

/// <summary>Simple skip/take pagination wrapper used by list queries such as the live wall feed.</summary>
public class PaginatedList<T>
{
    public IReadOnlyCollection<T> Items { get; }

    public int PageNumber { get; }

    public int TotalPages { get; }

    public int TotalCount { get; }

    public PaginatedList(IReadOnlyCollection<T> items, int count, int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        TotalPages = pageSize <= 0 ? 0 : (int)Math.Ceiling(count / (double)pageSize);
        TotalCount = count;
        Items = items;
    }

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;

    public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var count = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<T>(items, count, pageNumber, pageSize);
    }
}
