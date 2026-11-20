namespace QualityControl.Application.Common.Models;

/// <summary>
/// One page of results plus the metadata a client needs to render a pager.
/// </summary>
/// <typeparam name="T">Item type, always a DTO — entities never leave the Application layer.</typeparam>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>Total rows matching the filter, ignoring paging.</summary>
    public int TotalCount { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;

    public PagedResult()
    {
    }

    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    public static PagedResult<T> Empty(int page, int pageSize) => new([], 0, page, pageSize);
}
