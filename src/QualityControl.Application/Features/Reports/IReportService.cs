using QualityControl.Application.Features.Reports.Dtos;

namespace QualityControl.Application.Features.Reports;

public interface IReportService
{
    /// <summary>Builds the whole dashboard for the requested period.</summary>
    Task<DashboardDto> GetDashboardAsync(
        ReportPeriodQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DepartmentStatisticsDto>> GetDepartmentStatisticsAsync(
        ReportPeriodQuery query,
        CancellationToken cancellationToken = default);

    /// <param name="top">Maximum rows to return, highest count first.</param>
    Task<IReadOnlyList<DefectTypeStatisticsDto>> GetTopDefectTypesAsync(
        ReportPeriodQuery query,
        int top = 10,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MonthlyTrendPointDto>> GetMonthlyTrendAsync(
        ReportPeriodQuery query,
        CancellationToken cancellationToken = default);
}
