using Microsoft.EntityFrameworkCore;
using QualityControl.Application.Common.Extensions;
using QualityControl.Application.Common.Interfaces;
using QualityControl.Application.Features.Reports.Dtos;
using QualityControl.Domain.Entities;
using QualityControl.Domain.Enums;

namespace QualityControl.Application.Features.Reports;

/// <inheritdoc cref="IReportService"/>
public class ReportService : IReportService
{
    /// <summary>How far back the dashboard looks when the caller names no period.</summary>
    private const int DefaultPeriodMonths = 12;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public ReportService(IUnitOfWork unitOfWork, IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<DashboardDto> GetDashboardAsync(
        ReportPeriodQuery query,
        CancellationToken cancellationToken = default)
    {
        var (from, to) = ResolvePeriod(query);

        // Sequential, not parallel: these share one DbContext, and a DbContext does
        // not support concurrent operations.
        var summary = await GetSummaryAsync(query, from, to, cancellationToken);
        var byDepartment = await GetDepartmentStatisticsAsync(query, cancellationToken);
        var topDefects = await GetTopDefectTypesAsync(query, 10, cancellationToken);
        var trend = await GetMonthlyTrendAsync(query, cancellationToken);

        return new DashboardDto
        {
            PeriodFrom = from,
            PeriodTo = to,
            Summary = summary,
            ByDepartment = byDepartment,
            TopDefectTypes = topDefects,
            MonthlyTrend = trend
        };
    }

    private async Task<DashboardSummaryDto> GetSummaryAsync(
        ReportPeriodQuery query,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        var inspections = BuildInspectionQuery(query, from, to);

        // One round-trip for all inspection counters, aggregated by the database.
        var totals = await inspections
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalInspections = g.Count(),
                TotalBatches = g.Select(i => i.ProductionBatchId).Distinct().Count(),
                UnitsInspected = g.Sum(i => i.QuantityInspected),
                UnitsRejected = g.Sum(i => i.QuantityRejected),
                Conforming = g.Count(i => i.Outcome == InspectionOutcome.Conforming),
                Rework = g.Count(i => i.Outcome == InspectionOutcome.ReworkRequired),
                Defective = g.Count(i => i.Outcome == InspectionOutcome.Defective)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var defects = BuildDefectQuery(query, from, to);

        var openDefects = await defects
            .Where(d => !d.CorrectiveActions.Any()
                        || d.CorrectiveActions.Any(a => a.Status != CorrectiveActionStatus.Closed))
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Critical = g.Count(d => d.Severity == DefectSeverity.Critical)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var actions = BuildCorrectiveActionQuery(query, from, to);
        var today = _clock.UtcNow.Date;

        var actionTotals = await actions
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Closed = g.Count(a => a.Status == CorrectiveActionStatus.Closed),
                Overdue = g.Count(a =>
                    a.DueDate != null
                    && a.Status != CorrectiveActionStatus.Closed
                    && a.DueDate < today)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var unitsInspected = totals?.UnitsInspected ?? 0;
        var unitsRejected = totals?.UnitsRejected ?? 0;

        return new DashboardSummaryDto
        {
            TotalInspections = totals?.TotalInspections ?? 0,
            TotalBatchesInspected = totals?.TotalBatches ?? 0,
            TotalUnitsInspected = unitsInspected,
            TotalUnitsRejected = unitsRejected,
            DefectRatePercent = Percent(unitsRejected, unitsInspected),
            OpenDefects = openDefects?.Total ?? 0,
            CriticalOpenDefects = openDefects?.Critical ?? 0,
            ClosedCorrectiveActions = actionTotals?.Closed ?? 0,
            OverdueCorrectiveActions = actionTotals?.Overdue ?? 0,
            InspectionsConforming = totals?.Conforming ?? 0,
            InspectionsReworkRequired = totals?.Rework ?? 0,
            InspectionsDefective = totals?.Defective ?? 0
        };
    }

    public async Task<IReadOnlyList<DepartmentStatisticsDto>> GetDepartmentStatisticsAsync(
        ReportPeriodQuery query,
        CancellationToken cancellationToken = default)
    {
        var (from, to) = ResolvePeriod(query);

        var rows = await BuildInspectionQuery(query, from, to)
            .GroupBy(i => new { i.ProductionBatch.DepartmentId, i.ProductionBatch.Department.Name })
            .Select(g => new
            {
                g.Key.DepartmentId,
                g.Key.Name,
                InspectionCount = g.Count(),
                UnitsInspected = g.Sum(i => i.QuantityInspected),
                UnitsRejected = g.Sum(i => i.QuantityRejected),
                DefectCount = g.Sum(i => i.Defects.Count()),
                OpenDefectCount = g.Sum(i => i.Defects.Count(d =>
                    !d.CorrectiveActions.Any()
                    || d.CorrectiveActions.Any(a => a.Status != CorrectiveActionStatus.Closed)))
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new DepartmentStatisticsDto
            {
                DepartmentId = r.DepartmentId,
                DepartmentName = r.Name,
                InspectionCount = r.InspectionCount,
                UnitsInspected = r.UnitsInspected,
                UnitsRejected = r.UnitsRejected,
                DefectRatePercent = Percent(r.UnitsRejected, r.UnitsInspected),
                DefectCount = r.DefectCount,
                OpenDefectCount = r.OpenDefectCount
            })
            .OrderByDescending(r => r.DefectRatePercent)
            .ThenByDescending(r => r.UnitsInspected)
            .ToList();
    }

    public async Task<IReadOnlyList<DefectTypeStatisticsDto>> GetTopDefectTypesAsync(
        ReportPeriodQuery query,
        int top = 10,
        CancellationToken cancellationToken = default)
    {
        var (from, to) = ResolvePeriod(query);
        var defects = BuildDefectQuery(query, from, to);

        var totalDefects = await defects.CountAsync(cancellationToken);

        if (totalDefects == 0)
        {
            return [];
        }

        var rows = await defects
            .GroupBy(d => new { d.DefectTypeId, d.DefectType.Name, d.DefectType.Code })
            .Select(g => new
            {
                g.Key.DefectTypeId,
                g.Key.Name,
                g.Key.Code,
                DefectCount = g.Count(),
                AffectedUnits = g.Sum(d => d.AffectedQuantity),
                CriticalCount = g.Count(d => d.Severity == DefectSeverity.Critical)
            })
            .OrderByDescending(g => g.DefectCount)
            .Take(top)
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new DefectTypeStatisticsDto
            {
                DefectTypeId = r.DefectTypeId,
                DefectTypeName = r.Name,
                DefectTypeCode = r.Code,
                DefectCount = r.DefectCount,
                AffectedUnits = r.AffectedUnits,
                SharePercent = Percent(r.DefectCount, totalDefects),
                CriticalCount = r.CriticalCount
            })
            .ToList();
    }

    public async Task<IReadOnlyList<MonthlyTrendPointDto>> GetMonthlyTrendAsync(
        ReportPeriodQuery query,
        CancellationToken cancellationToken = default)
    {
        var (from, to) = ResolvePeriod(query);

        var rows = await BuildInspectionQuery(query, from, to)
            .GroupBy(i => new { i.InspectionDate.Year, i.InspectionDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                InspectionCount = g.Count(),
                UnitsInspected = g.Sum(i => i.QuantityInspected),
                UnitsRejected = g.Sum(i => i.QuantityRejected),
                DefectCount = g.Sum(i => i.Defects.Count())
            })
            .ToListAsync(cancellationToken);

        var byMonth = rows.ToDictionary(r => (r.Year, r.Month));
        var points = new List<MonthlyTrendPointDto>();

        // Walk every month in the period, not just the ones with data. A month with
        // no inspections must plot as zero, otherwise the line silently skips it and
        // the trend reads as continuous when it is not.
        var cursor = new DateTime(from.Year, from.Month, 1);
        var last = new DateTime(to.Year, to.Month, 1);

        while (cursor <= last)
        {
            if (byMonth.TryGetValue((cursor.Year, cursor.Month), out var row))
            {
                points.Add(new MonthlyTrendPointDto
                {
                    Year = row.Year,
                    Month = row.Month,
                    InspectionCount = row.InspectionCount,
                    UnitsInspected = row.UnitsInspected,
                    UnitsRejected = row.UnitsRejected,
                    DefectRatePercent = Percent(row.UnitsRejected, row.UnitsInspected),
                    DefectCount = row.DefectCount
                });
            }
            else
            {
                points.Add(new MonthlyTrendPointDto { Year = cursor.Year, Month = cursor.Month });
            }

            cursor = cursor.AddMonths(1);
        }

        return points;
    }

    /// <summary>
    /// Resolves the requested window, defaulting to the last twelve months.
    /// The upper bound is exclusive of the following day so the end date is included.
    /// </summary>
    private (DateTime From, DateTime To) ResolvePeriod(ReportPeriodQuery query)
    {
        var to = (query.DateTo ?? _clock.UtcNow).Date;
        var from = (query.DateFrom ?? to.AddMonths(-DefaultPeriodMonths)).Date;

        // Guard against an inverted range producing empty reports with no explanation.
        return from > to ? (to, from) : (from, to);
    }

    private IQueryable<QualityInspection> BuildInspectionQuery(
        ReportPeriodQuery query,
        DateTime from,
        DateTime to) =>
        _unitOfWork.Repository<QualityInspection>()
            .Query()
            .Where(i => i.InspectionDate >= from && i.InspectionDate < to.AddDays(1))
            .WhereIf(query.DepartmentId is not null, i => i.ProductionBatch.DepartmentId == query.DepartmentId)
            .WhereIf(query.ProductId is not null, i => i.ProductionBatch.ProductId == query.ProductId);

    private IQueryable<Defect> BuildDefectQuery(ReportPeriodQuery query, DateTime from, DateTime to) =>
        _unitOfWork.Repository<Defect>()
            .Query()
            .Where(d => d.DetectedAt >= from && d.DetectedAt < to.AddDays(1))
            .WhereIf(
                query.DepartmentId is not null,
                d => d.QualityInspection.ProductionBatch.DepartmentId == query.DepartmentId)
            .WhereIf(
                query.ProductId is not null,
                d => d.QualityInspection.ProductionBatch.ProductId == query.ProductId);

    private IQueryable<CorrectiveAction> BuildCorrectiveActionQuery(
        ReportPeriodQuery query,
        DateTime from,
        DateTime to) =>
        _unitOfWork.Repository<CorrectiveAction>()
            .Query()
            .Where(a => a.CreatedAt >= from && a.CreatedAt < to.AddDays(1))
            .WhereIf(
                query.DepartmentId is not null,
                a => a.Defect.QualityInspection.ProductionBatch.DepartmentId == query.DepartmentId)
            .WhereIf(
                query.ProductId is not null,
                a => a.Defect.QualityInspection.ProductionBatch.ProductId == query.ProductId);

    /// <summary>Percentage rounded to two decimals; zero when the denominator is zero.</summary>
    private static decimal Percent(int part, int total) =>
        total == 0 ? 0m : Math.Round((decimal)part * 100 / total, 2);
}
