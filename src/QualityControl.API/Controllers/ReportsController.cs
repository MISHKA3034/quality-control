using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QualityControl.Application.Features.CorrectiveActions.Dtos;
using QualityControl.Application.Features.Defects.Dtos;
using QualityControl.Application.Features.QualityInspections.Dtos;
using QualityControl.Application.Features.Reports;
using QualityControl.Application.Features.Reports.Dtos;

namespace QualityControl.API.Controllers;

/// <summary>
/// Аналитика, дашборд и выгрузка отчётов.
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IReportExportService _exportService;

    public ReportsController(IReportService reportService, IReportExportService exportService)
    {
        _reportService = reportService;
        _exportService = exportService;
    }

    /// <summary>Данные дашборда за период.</summary>
    /// <remarks>Без указания периода берутся последние 12 месяцев.</remarks>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardDto>> GetDashboard(
        [FromQuery] ReportPeriodQuery query,
        CancellationToken cancellationToken) =>
        Ok(await _reportService.GetDashboardAsync(query, cancellationToken));

    /// <summary>Статистика по подразделениям.</summary>
    [HttpGet("departments")]
    [ProducesResponseType(typeof(IReadOnlyList<DepartmentStatisticsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DepartmentStatisticsDto>>> GetDepartmentStatistics(
        [FromQuery] ReportPeriodQuery query,
        CancellationToken cancellationToken) =>
        Ok(await _reportService.GetDepartmentStatisticsAsync(query, cancellationToken));

    /// <summary>Наиболее частые виды дефектов.</summary>
    /// <param name="top">Сколько позиций вернуть (1–100).</param>
    [HttpGet("top-defect-types")]
    [ProducesResponseType(typeof(IReadOnlyList<DefectTypeStatisticsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DefectTypeStatisticsDto>>> GetTopDefectTypes(
        [FromQuery] ReportPeriodQuery query,
        [FromQuery] int top = 10,
        CancellationToken cancellationToken = default) =>
        Ok(await _reportService.GetTopDefectTypesAsync(query, Math.Clamp(top, 1, 100), cancellationToken));

    /// <summary>Динамика брака по месяцам.</summary>
    /// <remarks>Месяцы без проверок возвращаются с нулями, чтобы график не рвался.</remarks>
    [HttpGet("monthly-trend")]
    [ProducesResponseType(typeof(IReadOnlyList<MonthlyTrendPointDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MonthlyTrendPointDto>>> GetMonthlyTrend(
        [FromQuery] ReportPeriodQuery query,
        CancellationToken cancellationToken) =>
        Ok(await _reportService.GetMonthlyTrendAsync(query, cancellationToken));

    /// <summary>Выгрузка актов проверки.</summary>
    /// <param name="query">Те же фильтры, что и у списка актов.</param>
    /// <param name="format">excel или pdf.</param>
    [HttpGet("export/inspections")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportInspections(
        [FromQuery] QualityInspectionQuery query,
        [FromQuery] ExportFormat format = ExportFormat.Excel,
        CancellationToken cancellationToken = default) =>
        ToFileResult(await _exportService.ExportInspectionsAsync(query, format, cancellationToken));

    /// <summary>Выгрузка дефектов.</summary>
    [HttpGet("export/defects")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportDefects(
        [FromQuery] DefectQuery query,
        [FromQuery] ExportFormat format = ExportFormat.Excel,
        CancellationToken cancellationToken = default) =>
        ToFileResult(await _exportService.ExportDefectsAsync(query, format, cancellationToken));

    /// <summary>Выгрузка корректирующих действий.</summary>
    [HttpGet("export/corrective-actions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportCorrectiveActions(
        [FromQuery] CorrectiveActionQuery query,
        [FromQuery] ExportFormat format = ExportFormat.Excel,
        CancellationToken cancellationToken = default) =>
        ToFileResult(await _exportService.ExportCorrectiveActionsAsync(query, format, cancellationToken));

    /// <summary>Выгрузка статистики по подразделениям.</summary>
    [HttpGet("export/departments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportDepartmentStatistics(
        [FromQuery] ReportPeriodQuery query,
        [FromQuery] ExportFormat format = ExportFormat.Excel,
        CancellationToken cancellationToken = default) =>
        ToFileResult(await _exportService.ExportDepartmentStatisticsAsync(query, format, cancellationToken));

    /// <summary>Выгрузка отчёта по частым дефектам.</summary>
    [HttpGet("export/top-defect-types")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportTopDefectTypes(
        [FromQuery] ReportPeriodQuery query,
        [FromQuery] ExportFormat format = ExportFormat.Excel,
        CancellationToken cancellationToken = default) =>
        ToFileResult(await _exportService.ExportTopDefectTypesAsync(query, format, cancellationToken));

    private FileContentResult ToFileResult(ExportFile file) =>
        File(file.Content, file.ContentType, file.FileName);
}
