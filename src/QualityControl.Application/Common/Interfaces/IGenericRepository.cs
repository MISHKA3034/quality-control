using System.Linq.Expressions;
using QualityControl.Domain.Common;

namespace QualityControl.Application.Common.Interfaces;

/// <summary>
/// Persistence contract for one aggregate. Implemented by Infrastructure over EF Core.
/// </summary>
/// <typeparam name="TEntity">Entity type.</typeparam>
public interface IGenericRepository<TEntity>
    where TEntity : BaseEntity
{
    /// <summary>
    /// Composable query used by services to apply filtering, sorting and paging.
    /// Soft-deleted rows are already excluded by a global query filter.
    /// </summary>
    /// <param name="tracking">
    /// Leave false for reads: change tracking costs time and memory and is only
    /// needed when the loaded entity is going to be modified.
    /// </param>
    IQueryable<TEntity> Query(bool tracking = false);

    /// <summary>Loads one entity by primary key, tracked so the caller may modify it.</summary>
    Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool tracking = false,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    /// <summary>
    /// Removes the entity. For <see cref="ISoftDeletable"/> types this flags the row
    /// instead of deleting it, so quality records stay reconstructable.
    /// </summary>
    void Remove(TEntity entity);
}
