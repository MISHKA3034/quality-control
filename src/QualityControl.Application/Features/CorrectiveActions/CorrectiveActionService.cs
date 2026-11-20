using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QualityControl.Application.Common.Exceptions;
using QualityControl.Application.Common.Extensions;
using QualityControl.Application.Common.Interfaces;
using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.CorrectiveActions.Dtos;
using QualityControl.Domain.Entities;
using QualityControl.Domain.Enums;

namespace QualityControl.Application.Features.CorrectiveActions;

/// <inheritdoc cref="ICorrectiveActionService"/>
public class CorrectiveActionService : ICorrectiveActionService
{
    private static readonly Dictionary<string, Expression<Func<CorrectiveAction, object?>>> Sorts =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["createdAt"] = a => a.CreatedAt,
            ["dueDate"] = a => a.DueDate,
            ["status"] = a => a.Status,
            ["severity"] = a => a.Defect.Severity,
            ["assignedTo"] = a => a.AssignedTo!.LastName,
            ["batchNumber"] = a => a.Defect.QualityInspection.ProductionBatch.BatchNumber
        };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;
    private readonly INotificationService _notifications;
    private readonly ILogger<CorrectiveActionService> _logger;

    public CorrectiveActionService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAuditService audit,
        INotificationService notifications,
        ILogger<CorrectiveActionService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUser = currentUser;
        _clock = clock;
        _audit = audit;
        _notifications = notifications;
        _logger = logger;
    }

    public Task<PagedResult<CorrectiveActionListItemDto>> GetPagedAsync(
        CorrectiveActionQuery query,
        CancellationToken cancellationToken = default)
    {
        var source = BuildQuery(query)
            .ApplySort(query.SortBy, query.SortDescending, Sorts, a => a.CreatedAt, defaultDescending: true);

        return source.ToPagedResultAsync<CorrectiveAction, CorrectiveActionListItemDto>(
            _mapper.ConfigurationProvider,
            query,
            cancellationToken);
    }

    /// <summary>Shared by the list endpoint and the report exports.</summary>
    internal IQueryable<CorrectiveAction> BuildQuery(CorrectiveActionQuery query)
    {
        var search = query.Search?.Trim();
        var dateTo = query.DateTo?.Date.AddDays(1);
        var today = _clock.UtcNow.Date;

        return _unitOfWork.Repository<CorrectiveAction>()
            .Query()
            .WhereIf(query.DefectId is not null, a => a.DefectId == query.DefectId)
            .WhereIf(
                query.ProductionBatchId is not null,
                a => a.Defect.QualityInspection.ProductionBatchId == query.ProductionBatchId)
            .WhereIf(
                query.DepartmentId is not null,
                a => a.Defect.QualityInspection.ProductionBatch.DepartmentId == query.DepartmentId)
            .WhereIf(query.AssignedToId is not null, a => a.AssignedToId == query.AssignedToId)
            .WhereIf(query.Status is not null, a => a.Status == query.Status)
            .WhereIf(query.Severity is not null, a => a.Defect.Severity == query.Severity)
            .WhereIf(query.DateFrom is not null, a => a.CreatedAt >= query.DateFrom!.Value.Date)
            .WhereIf(dateTo is not null, a => a.CreatedAt < dateTo)
            .WhereIf(
                query.IsOverdue == true,
                a => a.DueDate != null && a.Status != CorrectiveActionStatus.Closed && a.DueDate < today)
            .WhereIf(
                query.IsOverdue == false,
                a => a.DueDate == null || a.Status == CorrectiveActionStatus.Closed || a.DueDate >= today)
            .WhereIf(
                !string.IsNullOrWhiteSpace(search),
                a => EF.Functions.Like(a.Description, $"%{search}%")
                     || EF.Functions.Like(a.Defect.Description, $"%{search}%")
                     || EF.Functions.Like(a.Defect.QualityInspection.ProductionBatch.BatchNumber, $"%{search}%"));
    }

    public async Task<IReadOnlyList<CorrectiveActionListItemDto>> GetForExportAsync(
        CorrectiveActionQuery query,
        int maxRows,
        CancellationToken cancellationToken = default) =>
        await BuildQuery(query)
            .ApplySort(query.SortBy, query.SortDescending, Sorts, a => a.CreatedAt, defaultDescending: true)
            .Take(maxRows)
            .ProjectTo<CorrectiveActionListItemDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

    public async Task<CorrectiveActionDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var dto = await _unitOfWork.Repository<CorrectiveAction>()
            .Query()
            .Where(a => a.Id == id)
            .ProjectTo<CorrectiveActionDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Корректирующее действие", id);

        dto.AllowedNextStatuses = CorrectiveAction.GetAllowedTransitions(dto.Status);
        return dto;
    }

    public async Task<CorrectiveActionDto> CreateAsync(
        CreateCorrectiveActionRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new ForbiddenException("Требуется аутентификация.");

        var defect = await _unitOfWork.Repository<Defect>()
            .Query()
            .Where(d => d.Id == request.DefectId)
            .Select(d => new
            {
                d.Id,
                d.Description,
                AssigneeEmail = (string?)null
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(CreateCorrectiveActionRequest.DefectId)] = ["Дефект не найден."]
            });

        if (request.AssignedToId is not null)
        {
            await EnsureAssigneeIsUsableAsync(request.AssignedToId.Value, cancellationToken);
        }

        var now = _clock.UtcNow;

        var action = new CorrectiveAction
        {
            DefectId = defect.Id,
            Description = request.Description.Trim(),
            DueDate = request.DueDate?.Date
        };

        action.RecordCreation(userId, now);

        if (request.AssignedToId is not null)
        {
            action.AssignTo(request.AssignedToId.Value, userId, now, "Ответственный назначен при создании.");
        }

        await _unitOfWork.Repository<CorrectiveAction>().AddAsync(action, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(
            AuditAction.Create,
            nameof(CorrectiveAction),
            action.Id.ToString(),
            $"Создано корректирующее действие по дефекту №{defect.Id}.",
            cancellationToken: cancellationToken);

        if (request.AssignedToId is not null)
        {
            await NotifyAssigneeAsync(action.Id, request.AssignedToId.Value, cancellationToken);
        }

        return await GetByIdAsync(action.Id, cancellationToken);
    }

    public async Task<CorrectiveActionDto> UpdateAsync(
        int id,
        UpdateCorrectiveActionRequest request,
        CancellationToken cancellationToken = default)
    {
        var action = await _unitOfWork.Repository<CorrectiveAction>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Корректирующее действие", id);

        if (action.Status is CorrectiveActionStatus.Closed)
        {
            throw new Domain.Exceptions.DomainException(
                "Закрытое корректирующее действие не может быть изменено.");
        }

        action.Description = request.Description.Trim();
        action.DueDate = request.DueDate?.Date;
        action.ResolutionNotes = request.ResolutionNotes?.Trim();

        _unitOfWork.Repository<CorrectiveAction>().Update(action);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<CorrectiveActionDto> AssignAsync(
        int id,
        AssignCorrectiveActionRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new ForbiddenException("Требуется аутентификация.");

        var action = await _unitOfWork.Repository<CorrectiveAction>()
            .Query(tracking: true)
            .Include(a => a.History)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException("Корректирующее действие", id);

        await EnsureAssigneeIsUsableAsync(request.AssignedToId, cancellationToken);

        action.AssignTo(request.AssignedToId, userId, _clock.UtcNow, request.Comment?.Trim());

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(
            AuditAction.Update,
            nameof(CorrectiveAction),
            id.ToString(),
            $"Назначен ответственный (сотрудник №{request.AssignedToId}).",
            cancellationToken: cancellationToken);

        await NotifyAssigneeAsync(id, request.AssignedToId, cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<CorrectiveActionDto> ChangeStatusAsync(
        int id,
        ChangeCorrectiveActionStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new ForbiddenException("Требуется аутентификация.");

        var action = await _unitOfWork.Repository<CorrectiveAction>()
            .Query(tracking: true)
            .Include(a => a.History)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException("Корректирующее действие", id);

        var previous = action.Status;
        int? verifiedByEmployeeId = null;

        // Acceptance is an OTK act: only a controller may declare the fix good.
        if (request.Status is CorrectiveActionStatus.Verified)
        {
            if (!_currentUser.IsInRole(RoleNames.QualityInspector)
                && !_currentUser.IsInRole(RoleNames.Administrator))
            {
                throw new ForbiddenException(
                    "Принять устранение дефекта может только контролёр ОТК.");
            }

            verifiedByEmployeeId = _currentUser.EmployeeId
                ?? throw new ForbiddenException(
                    "Учётная запись не связана с сотрудником и не может принимать работы.");
        }

        action.ChangeStatus(request.Status, userId, _clock.UtcNow, request.Comment?.Trim(), verifiedByEmployeeId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(
            AuditAction.CorrectiveActionStatusChanged,
            nameof(CorrectiveAction),
            id.ToString(),
            $"Статус: {previous} → {action.Status}. {request.Comment}".Trim(),
            cancellationToken: cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    private async Task EnsureAssigneeIsUsableAsync(int employeeId, CancellationToken cancellationToken)
    {
        var isUsable = await _unitOfWork.Repository<Employee>()
            .ExistsAsync(e => e.Id == employeeId && e.IsActive, cancellationToken);

        if (!isUsable)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(AssignCorrectiveActionRequest.AssignedToId)] =
                    ["Указанный сотрудник не найден или уволен."]
            });
        }
    }

    /// <summary>
    /// Tells the assignee they own the action. Never fails the request: the
    /// assignment itself is already committed and is what matters.
    /// </summary>
    private async Task NotifyAssigneeAsync(int actionId, int employeeId, CancellationToken cancellationToken)
    {
        try
        {
            var info = await _unitOfWork.Repository<CorrectiveAction>()
                .Query()
                .Where(a => a.Id == actionId)
                .Select(a => new
                {
                    a.DueDate,
                    DefectDescription = a.Defect.Description,
                    Email = a.AssignedTo!.Email,
                    Name = a.AssignedTo.LastName + " " + a.AssignedTo.FirstName
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (info?.Email is null)
            {
                _logger.LogInformation(
                    "У сотрудника {EmployeeId} не указан e-mail — уведомление о назначении не отправлено.",
                    employeeId);
                return;
            }

            await _notifications.SendCorrectiveActionAssignedAsync(
                info.Email,
                info.Name,
                actionId,
                info.DefectDescription,
                info.DueDate,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Не удалось отправить уведомление о назначении корректирующего действия {ActionId}.",
                actionId);
        }
    }
}
