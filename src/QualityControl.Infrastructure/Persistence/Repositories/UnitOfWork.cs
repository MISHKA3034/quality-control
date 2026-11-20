using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using QualityControl.Application.Common.Interfaces;
using QualityControl.Domain.Common;

namespace QualityControl.Infrastructure.Persistence.Repositories;

/// <inheritdoc cref="IUnitOfWork"/>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly ConcurrentDictionary<Type, object> _repositories = new();

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IGenericRepository<TEntity> Repository<TEntity>()
        where TEntity : BaseEntity =>
        (IGenericRepository<TEntity>)_repositories.GetOrAdd(
            typeof(TEntity),
            _ => new GenericRepository<TEntity>(_context));

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        // Joining an ambient transaction rather than nesting: EF does not support
        // nested transactions, and silently opening a second one would commit the
        // inner work independently of the outer.
        if (_context.Database.CurrentTransaction is not null)
        {
            return await operation(cancellationToken);
        }

        var strategy = _context.Database.CreateExecutionStrategy();

        // The execution strategy owns the retry loop, so the whole transaction is
        // replayed on a transient failure instead of half-committing.
        return await strategy.ExecuteAsync(async ct =>
        {
            await using IDbContextTransaction transaction =
                await _context.Database.BeginTransactionAsync(ct);

            try
            {
                var result = await operation(ct);
                await transaction.CommitAsync(ct);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        },
        cancellationToken);
    }
}
