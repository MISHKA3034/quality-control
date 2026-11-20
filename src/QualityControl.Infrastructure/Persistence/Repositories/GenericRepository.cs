using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using QualityControl.Application.Common.Interfaces;
using QualityControl.Domain.Common;

namespace QualityControl.Infrastructure.Persistence.Repositories;

/// <inheritdoc cref="IGenericRepository{TEntity}"/>
public class GenericRepository<TEntity> : IGenericRepository<TEntity>
    where TEntity : BaseEntity
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<TEntity> _set;

    public GenericRepository(ApplicationDbContext context)
    {
        _context = context;
        _set = context.Set<TEntity>();
    }

    public IQueryable<TEntity> Query(bool tracking = false) =>
        tracking ? _set : _set.AsNoTracking();

    public async Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _set.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool tracking = false,
        CancellationToken cancellationToken = default) =>
        await Query(tracking).FirstOrDefaultAsync(predicate, cancellationToken);

    public async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        await _set.AsNoTracking().AnyAsync(predicate, cancellationToken);

    public async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default) =>
        predicate is null
            ? await _set.AsNoTracking().CountAsync(cancellationToken)
            : await _set.AsNoTracking().CountAsync(predicate, cancellationToken);

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await _set.AddAsync(entity, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) =>
        await _set.AddRangeAsync(entities, cancellationToken);

    public void Update(TEntity entity) => _set.Update(entity);

    /// <summary>
    /// Marks the entity for removal. Soft-deletable types are turned into flag
    /// updates by <see cref="ApplicationDbContext.SaveChangesAsync"/>, so callers
    /// do not have to know which kind they are holding.
    /// </summary>
    public void Remove(TEntity entity) => _set.Remove(entity);
}
