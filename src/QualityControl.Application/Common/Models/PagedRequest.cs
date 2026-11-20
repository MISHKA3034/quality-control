namespace QualityControl.Application.Common.Models;

/// <summary>
/// Base for every list query: paging plus sorting. Feature-specific filters are
/// added by deriving from this type.
/// </summary>
public abstract class PagedRequest
{
    /// <summary>Largest page a client may request, so no caller can pull the whole table in one call.</summary>
    public const int MaxPageSize = 200;

    public const int DefaultPageSize = 20;

    private int _page = 1;
    private int _pageSize = DefaultPageSize;

    /// <summary>1-based page number. Values below 1 are clamped rather than rejected.</summary>
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }

    /// <summary>
    /// Property to sort by. Matched case-insensitively against a per-endpoint
    /// allowlist; an unknown value falls back to that endpoint's default order.
    /// </summary>
    public string? SortBy { get; set; }

    public bool SortDescending { get; set; }

    public int Skip => (Page - 1) * PageSize;
}
