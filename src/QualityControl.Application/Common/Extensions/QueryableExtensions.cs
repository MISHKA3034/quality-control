using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using QualityControl.Application.Common.Models;

namespace QualityControl.Application.Common.Extensions;

/// <summary>
/// Query helpers shared by every list endpoint: allowlisted sorting and paging that
/// both run in SQL.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Orders by the caller's <paramref name="sortBy"/> if it matches
    /// <paramref name="allowedSorts"/>, otherwise by <paramref name="defaultSort"/>.
    ///
    /// The allowlist is the security boundary: sort keys arrive from the query string,
    /// so mapping them through a fixed dictionary — rather than reflecting over the
    /// supplied name — keeps a caller from ordering by a column that is not theirs to
    /// see, or from probing the schema through error messages.
    /// </summary>
    /// <typeparam name="T">Entity type being queried.</typeparam>
    /// <param name="query">Source query.</param>
    /// <param name="sortBy">Requested sort key; matched case-insensitively.</param>
    /// <param name="descending">Sort direction.</param>
    /// <param name="allowedSorts">Sort key to key-selector map for this endpoint.</param>
    /// <param name="defaultSort">Applied when <paramref name="sortBy"/> is absent or unknown.</param>
    /// <param name="defaultDescending">Direction for the default sort.</param>
    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> query,
        string? sortBy,
        bool descending,
        IReadOnlyDictionary<string, Expression<Func<T, object?>>> allowedSorts,
        Expression<Func<T, object?>> defaultSort,
        bool defaultDescending = false)
    {
        if (!string.IsNullOrWhiteSpace(sortBy)
            && allowedSorts.TryGetValue(sortBy.Trim(), out var selector))
        {
            return descending ? query.OrderByDescending(selector) : query.OrderBy(selector);
        }

        return defaultDescending ? query.OrderByDescending(defaultSort) : query.OrderBy(defaultSort);
    }

    /// <summary>
    /// Projects to <typeparamref name="TDto"/> and returns the requested page.
    ///
    /// <c>ProjectTo</c> is what makes this efficient: AutoMapper rewrites the mapping
    /// into the SELECT, so only the DTO's columns are read and no entity is
    /// materialised or tracked.
    /// </summary>
    /// <typeparam name="TSource">Entity type being queried.</typeparam>
    /// <typeparam name="TDto">Projection target.</typeparam>
    /// <param name="query">Source query, already filtered and sorted.</param>
    /// <param name="configuration">AutoMapper configuration used for the projection.</param>
    /// <param name="request">Paging parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task<PagedResult<TDto>> ToPagedResultAsync<TSource, TDto>(
        this IQueryable<TSource> query,
        IConfigurationProvider configuration,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        // Skip the second round-trip when the filter matched nothing.
        if (totalCount == 0)
        {
            return PagedResult<TDto>.Empty(request.Page, request.PageSize);
        }

        var items = await query
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ProjectTo<TDto>(configuration)
            .ToListAsync(cancellationToken);

        return new PagedResult<TDto>(items, totalCount, request.Page, request.PageSize);
    }

    /// <summary>
    /// Applies <paramref name="predicate"/> only when <paramref name="condition"/> holds,
    /// so optional filters read as a flat chain instead of nested ifs.
    /// </summary>
    /// <typeparam name="T">Entity type being queried.</typeparam>
    public static IQueryable<T> WhereIf<T>(
        this IQueryable<T> query,
        bool condition,
        Expression<Func<T, bool>> predicate) =>
        condition ? query.Where(predicate) : query;
}
