using QualityControl.Application.Features.CorrectiveActions.Dtos;
using QualityControl.Application.Features.Defects.Dtos;
using QualityControl.Application.Features.QualityInspections.Dtos;
using QualityControl.Application.Features.Reports.Dtos;

namespace QualityControl.Application.Features.Reports;

/// <summary>Output format for an exported report.</summary>
public enum ExportFormat
{
    Excel = 0,
    Pdf = 1
}

/// <summary>A rendered report, ready to be written to the response.</summary>
/// <param name="Content">File bytes.</param>
/// <param name="ContentType">Media type to send.</param>
/// <param name="FileName">Suggested download name.</param>
public sealed record ExportFile(byte[] Content, string ContentType, string FileName);

/// <summary>
/// Builds report files. The column layout of each report lives here — a reporting
/// decision, not a transport one — so Excel and PDF cannot drift apart.
/// </summary>
public interface IReportExportService
{
    Task<ExportFile> ExportInspectionsAsync(
        QualityInspectionQuery query,
        ExportFormat format,
        CancellationToken cancellationToken = default);

    Task<ExportFile> ExportDefectsAsync(
        DefectQuery query,
        ExportFormat format,
        CancellationToken cancellationToken = default);

    Task<ExportFile> ExportCorrectiveActionsAsync(
        CorrectiveActionQuery query,
        ExportFormat format,
        CancellationToken cancellationToken = default);

    Task<ExportFile> ExportDepartmentStatisticsAsync(
        ReportPeriodQuery query,
        ExportFormat format,
        CancellationToken cancellationToken = default);

    Task<ExportFile> ExportTopDefectTypesAsync(
        ReportPeriodQuery query,
        ExportFormat format,
        CancellationToken cancellationToken = default);
}
