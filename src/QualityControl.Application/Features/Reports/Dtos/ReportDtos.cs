namespace QualityControl.Application.Features.Reports.Dtos;

/// <summary>
/// Period the dashboard covers. Defaults to the last 12 months when unspecified.
/// </summary>
public class ReportPeriodQuery
{
    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }

    public int? DepartmentId { get; set; }

    public int? ProductId { get; set; }
}

/// <summary>The headline counters.</summary>
public class DashboardSummaryDto
{
    public int TotalInspections { get; init; }

    public int TotalBatchesInspected { get; init; }

    public int TotalUnitsInspected { get; init; }

    public int TotalUnitsRejected { get; init; }

    /// <summary>
    /// Процент брака — rejected units over inspected units, not acts over acts.
    /// Counting acts would let a 1000-unit batch weigh the same as a 5-unit one.
    /// </summary>
    public decimal DefectRatePercent { get; init; }

    public int OpenDefects { get; init; }

    public int CriticalOpenDefects { get; init; }

    public int ClosedCorrectiveActions { get; init; }

    public int OverdueCorrectiveActions { get; init; }

    public int InspectionsConforming { get; init; }

    public int InspectionsReworkRequired { get; init; }

    public int InspectionsDefective { get; init; }
}

/// <summary>Statistics for one department.</summary>
public class DepartmentStatisticsDto
{
    public int DepartmentId { get; init; }

    public string DepartmentName { get; init; } = null!;

    public int InspectionCount { get; init; }

    public int UnitsInspected { get; init; }

    public int UnitsRejected { get; init; }

    public decimal DefectRatePercent { get; init; }

    public int DefectCount { get; init; }

    public int OpenDefectCount { get; init; }
}

/// <summary>One row of the "most frequent defects" report.</summary>
public class DefectTypeStatisticsDto
{
    public int DefectTypeId { get; init; }

    public string DefectTypeName { get; init; } = null!;

    public string DefectTypeCode { get; init; } = null!;

    public int DefectCount { get; init; }

    public int AffectedUnits { get; init; }

    /// <summary>Share of all defects in the period.</summary>
    public decimal SharePercent { get; init; }

    public int CriticalCount { get; init; }
}

/// <summary>One point on the monthly defect-rate trend.</summary>
public class MonthlyTrendPointDto
{
    public int Year { get; init; }

    public int Month { get; init; }

    /// <summary>Label for the chart axis, e.g. "07.2026".</summary>
    public string Label => $"{Month:D2}.{Year}";

    public int InspectionCount { get; init; }

    public int UnitsInspected { get; init; }

    public int UnitsRejected { get; init; }

    public decimal DefectRatePercent { get; init; }

    public int DefectCount { get; init; }
}

/// <summary>Everything the dashboard screen renders, in one response.</summary>
public class DashboardDto
{
    public DateTime PeriodFrom { get; init; }

    public DateTime PeriodTo { get; init; }

    public DashboardSummaryDto Summary { get; init; } = null!;

    public IReadOnlyList<DepartmentStatisticsDto> ByDepartment { get; init; } = [];

    public IReadOnlyList<DefectTypeStatisticsDto> TopDefectTypes { get; init; } = [];

    public IReadOnlyList<MonthlyTrendPointDto> MonthlyTrend { get; init; } = [];
}
