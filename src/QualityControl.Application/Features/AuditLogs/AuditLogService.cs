using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using QualityControl.Application.Common.Extensions;
using QualityControl.Application.Common.Interfaces;
using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.AuditLogs.Dtos;
using QualityControl.Domain.Entities;

namespace QualityControl.Application.Features.AuditLogs;

/// <inheritdoc cref="IAuditLogService"/>
public class AuditLogService : IAuditLogService
{
    private static readonly Dictionary<string, Expression<Func<AuditLog, object?>>> Sorts =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["timestamp"] = l => l.Timestamp,
            ["userName"] = l => l.UserName,
            ["action"] = l => l.Action,
            ["entityName"] = l => l.EntityName
        };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AuditLogService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public Task<PagedResult<AuditLogDto>> GetPagedAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default)
    {
        var source = BuildQuery(query)
            .ApplySort(query.SortBy, query.SortDescending, Sorts, l => l.Timestamp, defaultDescending: true);

        return source.ToPagedResultAsync<AuditLog, AuditLogDto>(
            _mapper.ConfigurationProvider,
            query,
            cancellationToken);
    }

    /// <summary>Shared by the list endpoint and the audit export.</summary>
    internal IQueryable<AuditLog> BuildQuery(AuditLogQuery query)
    {
        var search = query.Search?.Trim();
        var entityName = query.EntityName?.Trim();
        var dateTo = query.DateTo?.Date.AddDays(1);

        return _unitOfWork.Repository<AuditLog>()
            .Query()
            .WhereIf(query.UserId is not null, l => l.UserId == query.UserId)
            .WhereIf(query.Action is not null, l => l.Action == query.Action)
            .WhereIf(!string.IsNullOrWhiteSpace(entityName), l => l.EntityName == entityName)
            .WhereIf(!string.IsNullOrWhiteSpace(query.EntityId), l => l.EntityId == query.EntityId)
            .WhereIf(query.IsSuccess is not null, l => l.IsSuccess == query.IsSuccess)
            .WhereIf(query.DateFrom is not null, l => l.Timestamp >= query.DateFrom!.Value.Date)
            .WhereIf(dateTo is not null, l => l.Timestamp < dateTo)
            .WhereIf(
                !string.IsNullOrWhiteSpace(search),
                l => (l.Description != null && EF.Functions.Like(l.Description, $"%{search}%"))
                     || (l.UserName != null && EF.Functions.Like(l.UserName, $"%{search}%"))
                     || (l.EntityId != null && EF.Functions.Like(l.EntityId, $"%{search}%")));
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetEntityHistoryAsync(
        string entityName,
        string entityId,
        CancellationToken cancellationToken = default) =>
        await _unitOfWork.Repository<AuditLog>()
            .Query()
            .Where(l => l.EntityName == entityName && l.EntityId == entityId)
            .OrderByDescending(l => l.Timestamp)
            .ProjectTo<AuditLogDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
}
