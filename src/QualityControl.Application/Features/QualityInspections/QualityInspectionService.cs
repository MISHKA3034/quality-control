using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QualityControl.Application.Common.Exceptions;
using QualityControl.Application.Common.Extensions;
using QualityControl.Application.Common.Interfaces;
using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.QualityInspections.Dtos;
using QualityControl.Domain.Entities;
using QualityControl.Domain.Enums;
using QualityControl.Domain.Exceptions;

namespace QualityControl.Application.Features.QualityInspections;

/// <inheritdoc cref="IQualityInspectionService"/>
public class QualityInspectionService : IQualityInspectionService
{
    private static readonly Dictionary<string, Expression<Func<QualityInspection, object?>>> Sorts =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["inspectionNumber"] = i => i.InspectionNumber,
            ["inspectionDate"] = i => i.InspectionDate,
            ["batchNumber"] = i => i.ProductionBatch.BatchNumber,
            ["product"] = i => i.ProductionBatch.Product.Name,
            ["inspector"] = i => i.Inspector.LastName,
            ["outcome"] = i => i.Outcome
        };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;
    private readonly INotificationService _notifications;
    private readonly ILogger<QualityInspectionService> _logger;

    public QualityInspectionService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAuditService audit,
        INotificationService notifications,
        ILogger<QualityInspectionService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUser = currentUser;
        _clock = clock;
        _audit = audit;
        _notifications = notifications;
        _logger = logger;
    }

    public Task<PagedResult<QualityInspectionListItemDto>> GetPagedAsync(
        QualityInspectionQuery query,
        CancellationToken cancellationToken = default)
    {
        var source = BuildQuery(query)
            .ApplySort(query.SortBy, query.SortDescending, Sorts, i => i.InspectionDate, defaultDescending: true);

        return source.ToPagedResultAsync<QualityInspection, QualityInspectionListItemDto>(
            _mapper.ConfigurationProvider,
            query,
            cancellationToken);
    }

    /// <summary>Shared by the list endpoint and the report exports.</summary>
    internal IQueryable<QualityInspection> BuildQuery(QualityInspectionQuery query)
    {
        var search = query.Search?.Trim();
        var dateTo = query.DateTo?.Date.AddDays(1);

        return _unitOfWork.Repository<QualityInspection>()
            .Query()
            .WhereIf(query.ProductionBatchId is not null, i => i.ProductionBatchId == query.ProductionBatchId)
            .WhereIf(query.ProductId is not null, i => i.ProductionBatch.ProductId == query.ProductId)
            .WhereIf(query.InspectorId is not null, i => i.InspectorId == query.InspectorId)
            .WhereIf(query.DepartmentId is not null, i => i.ProductionBatch.DepartmentId == query.DepartmentId)
            .WhereIf(query.Outcome is not null, i => i.Outcome == query.Outcome)
            .WhereIf(query.DateFrom is not null, i => i.InspectionDate >= query.DateFrom!.Value.Date)
            .WhereIf(dateTo is not null, i => i.InspectionDate < dateTo)
            .WhereIf(
                query.DefectTypeId is not null,
                i => i.Defects.Any(d => d.DefectTypeId == query.DefectTypeId))
            .WhereIf(
                !string.IsNullOrWhiteSpace(search),
                i => EF.Functions.Like(i.InspectionNumber, $"%{search}%")
                     || EF.Functions.Like(i.ProductionBatch.BatchNumber, $"%{search}%")
                     || EF.Functions.Like(i.ProductionBatch.Product.Name, $"%{search}%"));
    }

    public async Task<IReadOnlyList<QualityInspectionListItemDto>> GetForExportAsync(
        QualityInspectionQuery query,
        int maxRows,
        CancellationToken cancellationToken = default) =>
        await BuildQuery(query)
            .ApplySort(query.SortBy, query.SortDescending, Sorts, i => i.InspectionDate, defaultDescending: true)
            .Take(maxRows)
            .ProjectTo<QualityInspectionListItemDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

    public async Task<QualityInspectionDto> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _unitOfWork.Repository<QualityInspection>()
            .Query()
            .Where(i => i.Id == id)
            .ProjectTo<QualityInspectionDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("Акт проверки", id);

    public async Task<QualityInspectionDto> CreateAsync(
        CreateQualityInspectionRequest request,
        CancellationToken cancellationToken = default)
    {
        // The act is signed by the person filing it. Taking the inspector from the
        // payload would let one inspector file an act under another's name.
        var inspectorId = _currentUser.EmployeeId
            ?? throw new ForbiddenException(
                "Учётная запись не связана с сотрудником и не может подписывать акты проверки. "
                + "Обратитесь к администратору.");

        var inspectionId = await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                var batch = await _unitOfWork.Repository<ProductionBatch>()
                    .GetByIdAsync(request.ProductionBatchId, ct)
                    ?? throw new ValidationException(new Dictionary<string, string[]>
                    {
                        [nameof(CreateQualityInspectionRequest.ProductionBatchId)] = ["Партия не найдена."]
                    });

                batch.EnsureCanBeInspected();

                var inspection = new QualityInspection
                {
                    InspectionNumber = await GenerateInspectionNumberAsync(request.InspectionDate, ct),
                    ProductionBatchId = batch.Id,
                    InspectorId = inspectorId,
                    InspectionDate = request.InspectionDate,
                    Outcome = request.Outcome,
                    Comment = request.Comment?.Trim(),
                    QuantityInspected = request.QuantityInspected,
                    QuantityAccepted = request.QuantityAccepted,
                    QuantityRejected = request.QuantityRejected
                };

                // The act must be internally consistent before it touches the batch.
                inspection.ValidateQuantities(batch.Quantity);

                await ValidateOutcomeAgainstDefectsAsync(request, ct);

                foreach (var result in request.Results)
                {
                    inspection.Results.Add(new InspectionResult
                    {
                        ParameterName = result.ParameterName.Trim(),
                        ExpectedValue = result.ExpectedValue?.Trim(),
                        ActualValue = result.ActualValue?.Trim(),
                        UnitOfMeasure = result.UnitOfMeasure?.Trim(),
                        Outcome = result.Outcome,
                        Comment = result.Comment?.Trim(),
                        SortOrder = result.SortOrder
                    });
                }

                foreach (var defect in request.Defects)
                {
                    inspection.Defects.Add(new Defect
                    {
                        DefectTypeId = defect.DefectTypeId,
                        Description = defect.Description.Trim(),
                        Severity = defect.Severity,
                        Location = defect.Location?.Trim(),
                        Comment = defect.Comment?.Trim(),
                        AffectedQuantity = defect.AffectedQuantity,
                        DetectedAt = request.InspectionDate
                    });
                }

                await _unitOfWork.Repository<QualityInspection>().AddAsync(inspection, ct);

                // Walk the batch to UnderInspection along a legal path, then let the
                // verdict decide where it lands.
                MoveBatchUnderInspection(batch);
                batch.ApplyInspectionOutcome(request.Outcome);
                _unitOfWork.Repository<ProductionBatch>().Update(batch);

                await _unitOfWork.SaveChangesAsync(ct);

                await _audit.LogAsync(
                    AuditAction.Inspection,
                    nameof(QualityInspection),
                    inspection.Id.ToString(),
                    $"Оформлен акт {inspection.InspectionNumber} по партии {batch.BatchNumber}. "
                    + $"Результат: {request.Outcome}. Статус партии: {batch.Status}.",
                    cancellationToken: ct);

                return inspection.Id;
            },
            cancellationToken);

        await NotifyIfCriticalAsync(inspectionId, cancellationToken);

        return await GetByIdAsync(inspectionId, cancellationToken);
    }

    public async Task<QualityInspectionDto> UpdateAsync(
        int id,
        UpdateQualityInspectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var inspection = await _unitOfWork.Repository<QualityInspection>()
            .Query(tracking: true)
            .Include(i => i.Results)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new NotFoundException("Акт проверки", id);

        EnsureCanEdit(inspection);

        inspection.InspectionDate = request.InspectionDate;
        inspection.Comment = request.Comment?.Trim();

        // Result lines are a set owned by the act: replacing them wholesale matches
        // how the form posts them and avoids a per-line diff the client never sends.
        inspection.Results.Clear();

        foreach (var result in request.Results)
        {
            inspection.Results.Add(new InspectionResult
            {
                QualityInspectionId = inspection.Id,
                ParameterName = result.ParameterName.Trim(),
                ExpectedValue = result.ExpectedValue?.Trim(),
                ActualValue = result.ActualValue?.Trim(),
                UnitOfMeasure = result.UnitOfMeasure?.Trim(),
                Outcome = result.Outcome,
                Comment = result.Comment?.Trim(),
                SortOrder = result.SortOrder
            });
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(
            AuditAction.Update,
            nameof(QualityInspection),
            id.ToString(),
            $"Изменён акт {inspection.InspectionNumber}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var inspection = await _unitOfWork.Repository<QualityInspection>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Акт проверки", id);

        var hasCorrectiveActions = await _unitOfWork.Repository<CorrectiveAction>()
            .ExistsAsync(a => a.Defect.QualityInspectionId == id, cancellationToken);

        if (hasCorrectiveActions)
        {
            throw new DomainException(
                $"По дефектам акта {inspection.InspectionNumber} созданы корректирующие действия. "
                + "Удаление невозможно.");
        }

        _unitOfWork.Repository<QualityInspection>().Remove(inspection);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(
            AuditAction.Delete,
            nameof(QualityInspection),
            id.ToString(),
            $"Удалён акт {inspection.InspectionNumber}.",
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// An act may only be corrected by its author. Administrators are exempt so a
    /// genuine mistake is still fixable after the inspector leaves.
    /// </summary>
    private void EnsureCanEdit(QualityInspection inspection)
    {
        if (_currentUser.IsInRole(RoleNames.Administrator))
        {
            return;
        }

        if (_currentUser.EmployeeId != inspection.InspectorId)
        {
            throw new ForbiddenException(
                $"Акт {inspection.InspectionNumber} подписан другим контролёром и не может быть изменён.");
        }
    }

    /// <summary>
    /// Advances the batch to <see cref="BatchStatus.UnderInspection"/> through the
    /// transitions the domain permits, so an inspector never has to click the batch
    /// through intermediate states by hand.
    /// </summary>
    private static void MoveBatchUnderInspection(ProductionBatch batch)
    {
        if (batch.Status is BatchStatus.InProduction or BatchStatus.ReworkRequired)
        {
            batch.TransitionTo(BatchStatus.AwaitingInspection);
        }

        if (batch.Status is BatchStatus.AwaitingInspection)
        {
            batch.TransitionTo(BatchStatus.UnderInspection);
        }

        if (batch.Status is not BatchStatus.UnderInspection)
        {
            throw new DomainException(
                $"Партия {batch.BatchNumber} в статусе «{batch.Status}» — решение по ней уже принято. "
                + "Новый акт оформить нельзя.");
        }
    }

    /// <summary>
    /// The verdict and the findings must agree: an act that rejects units without
    /// naming a defect is not auditable.
    /// </summary>
    private async Task ValidateOutcomeAgainstDefectsAsync(
        CreateQualityInspectionRequest request,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Outcome is InspectionOutcome.Conforming && request.Defects.Count > 0)
        {
            errors[nameof(CreateQualityInspectionRequest.Outcome)] =
                ["Результат «Соответствует требованиям» несовместим с зарегистрированными дефектами."];
        }

        if (request.Outcome is not InspectionOutcome.Conforming && request.Defects.Count == 0)
        {
            errors[nameof(CreateQualityInspectionRequest.Defects)] =
                ["Для результата «Требуется доработка» или «Брак» необходимо указать хотя бы один дефект."];
        }

        if (request.Outcome is InspectionOutcome.Conforming && request.QuantityRejected > 0)
        {
            errors[nameof(CreateQualityInspectionRequest.QuantityRejected)] =
                ["Результат «Соответствует требованиям» несовместим с забракованными изделиями."];
        }

        if (request.Defects.Count > 0)
        {
            var typeIds = request.Defects.Select(d => d.DefectTypeId).Distinct().ToList();

            var knownIds = await _unitOfWork.Repository<DefectType>()
                .Query()
                .Where(t => typeIds.Contains(t.Id))
                .Select(t => t.Id)
                .ToListAsync(cancellationToken);

            if (knownIds.Count != typeIds.Count)
            {
                errors[nameof(CreateQualityInspectionRequest.Defects)] =
                    ["Указан несуществующий вид дефекта."];
            }
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    /// <summary>
    /// Builds the act number as АКТ-{year}-{ordinal}, continuing the year's sequence.
    /// </summary>
    /// <remarks>
    /// Two acts filed in the same instant can compute the same ordinal. The unique
    /// index on InspectionNumber turns that race into a failed transaction rather
    /// than a duplicate number, and the caller simply retries.
    /// </remarks>
    private async Task<string> GenerateInspectionNumberAsync(
        DateTime inspectionDate,
        CancellationToken cancellationToken)
    {
        var year = inspectionDate.Year;
        var prefix = $"АКТ-{year}-";

        var lastNumber = await _unitOfWork.Repository<QualityInspection>()
            .Query()
            .Where(i => i.InspectionNumber.StartsWith(prefix))
            .OrderByDescending(i => i.InspectionNumber)
            .Select(i => i.InspectionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var next = 1;

        if (lastNumber is not null
            && int.TryParse(lastNumber[prefix.Length..], out var lastOrdinal))
        {
            next = lastOrdinal + 1;
        }

        return $"{prefix}{next:D5}";
    }

    /// <summary>
    /// Warns OTK about critical findings. Runs after the transaction commits: a
    /// notification failure must not roll back a filed act.
    /// </summary>
    private async Task NotifyIfCriticalAsync(int inspectionId, CancellationToken cancellationToken)
    {
        try
        {
            var critical = await _unitOfWork.Repository<Defect>()
                .Query()
                .Where(d => d.QualityInspectionId == inspectionId && d.Severity == DefectSeverity.Critical)
                .Select(d => new
                {
                    d.Description,
                    BatchNumber = d.QualityInspection.ProductionBatch.BatchNumber,
                    ProductName = d.QualityInspection.ProductionBatch.Product.Name
                })
                .ToListAsync(cancellationToken);

            if (critical.Count == 0)
            {
                return;
            }

            var recipients = await _unitOfWork.Repository<User>()
                .Query()
                .Where(u => u.IsActive
                            && u.Role.Name == RoleNames.ProductionManager
                            && u.Email != null)
                .Select(u => u.Email)
                .ToListAsync(cancellationToken);

            foreach (var defect in critical)
            {
                await _notifications.SendCriticalDefectRegisteredAsync(
                    recipients,
                    defect.BatchNumber,
                    defect.ProductName,
                    defect.Description,
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Не удалось отправить уведомление о критическом дефекте по акту {InspectionId}. Акт сохранён.",
                inspectionId);
        }
    }
}
