using QualityControl.Domain.Common;

namespace QualityControl.Application.Common.Interfaces;

/// <summary>
/// One database session shared by all repositories resolved from it, committed as
/// a single transaction.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Repository for <typeparamref name="TEntity"/>. Instances are cached per unit
    /// of work, so every caller in a request shares one change tracker.
    /// </summary>
    IGenericRepository<TEntity> Repository<TEntity>()
        where TEntity : BaseEntity;

    /// <summary>
    /// Persists all pending changes, stamping audit fields on the way.
    /// </summary>
    /// <returns>Number of affected rows.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="operation"/> inside an explicit transaction, committing
    /// on success and rolling back on any exception. Needed where one use case makes
    /// several <see cref="SaveChangesAsync"/> calls that must succeed or fail together
    /// — filing an inspection act and updating its batch status, for instance.
    /// </summary>
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default);
}
