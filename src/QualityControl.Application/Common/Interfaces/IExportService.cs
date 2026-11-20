using QualityControl.Application.Common.Models;

namespace QualityControl.Application.Common.Interfaces;

/// <summary>
/// Renders a report table to XLSX.
/// </summary>
public interface IExcelExportService
{
    /// <summary>Media type to return with the produced bytes.</summary>
    const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    byte[] Export<T>(ExportTable<T> table);
}

/// <summary>
/// Renders a report table to PDF.
/// </summary>
public interface IPdfExportService
{
    const string ContentType = "application/pdf";

    byte[] Export<T>(ExportTable<T> table);
}
