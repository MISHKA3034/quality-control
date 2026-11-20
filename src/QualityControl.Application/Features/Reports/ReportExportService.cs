using QualityControl.Application.Common.Interfaces;
using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.CorrectiveActions;
using QualityControl.Application.Features.CorrectiveActions.Dtos;
using QualityControl.Application.Features.Defects;
using QualityControl.Application.Features.Defects.Dtos;
using QualityControl.Application.Features.QualityInspections;
using QualityControl.Application.Features.QualityInspections.Dtos;
using QualityControl.Application.Features.Reports.Dtos;
using QualityControl.Domain.Enums;

namespace QualityControl.Application.Features.Reports;

/// <inheritdoc cref="IReportExportService"/>
public class ReportExportService : IReportExportService
{
    /// <summary>
    /// Row ceiling for any export. Generous enough for a year of real data, low
    /// enough that one request cannot exhaust memory building a workbook.
    /// </summary>
    private const int MaxExportRows = 20_000;

    private readonly IQualityInspectionService _inspections;
    private readonly IDefectService _defects;
    private readonly ICorrectiveActionService _correctiveActions;
    private readonly IReportService _reports;
    private readonly IExcelExportService _excel;
    private readonly IPdfExportService _pdf;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;

    public ReportExportService(
        IQualityInspectionService inspections,
        IDefectService defects,
        ICorrectiveActionService correctiveActions,
        IReportService reports,
        IExcelExportService excel,
        IPdfExportService pdf,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAuditService audit)
    {
        _inspections = inspections;
        _defects = defects;
        _correctiveActions = correctiveActions;
        _reports = reports;
        _excel = excel;
        _pdf = pdf;
        _currentUser = currentUser;
        _clock = clock;
        _audit = audit;
    }

    public async Task<ExportFile> ExportInspectionsAsync(
        QualityInspectionQuery query,
        ExportFormat format,
        CancellationToken cancellationToken = default)
    {
        var rows = await _inspections.GetForExportAsync(query, MaxExportRows, cancellationToken);

        var table = new ExportTable<QualityInspectionListItemDto>
        {
            Title = "Акты проверки качества",
            Subtitle = DescribePeriod(query.DateFrom, query.DateTo),
            GeneratedBy = _currentUser.UserName,
            GeneratedAt = _clock.UtcNow,
            Rows = rows,
            Columns =
            [
                new("№ акта", r => r.InspectionNumber, RelativeWidth: 1.2f),
                new("Дата", r => r.InspectionDate, ExportColumnFormat.Date, 0.9f),
                new("Партия", r => r.BatchNumber, RelativeWidth: 1.2f),
                new("Изделие", r => r.ProductName, RelativeWidth: 2f),
                new("Участок", r => r.DepartmentName, RelativeWidth: 1.3f),
                new("Контролёр", r => r.InspectorName, RelativeWidth: 1.2f),
                new("Результат", r => Describe(r.Outcome), RelativeWidth: 1.3f),
                new("Проверено", r => r.QuantityInspected, ExportColumnFormat.Integer, 0.8f),
                new("Принято", r => r.QuantityAccepted, ExportColumnFormat.Integer, 0.8f),
                new("Брак", r => r.QuantityRejected, ExportColumnFormat.Integer, 0.7f),
                new("Дефектов", r => r.DefectCount, ExportColumnFormat.Integer, 0.8f)
            ]
        };

        return await RenderAsync(table, format, "Акты_проверки", cancellationToken);
    }

    public async Task<ExportFile> ExportDefectsAsync(
        DefectQuery query,
        ExportFormat format,
        CancellationToken cancellationToken = default)
    {
        var rows = await _defects.GetForExportAsync(query, MaxExportRows, cancellationToken);

        var table = new ExportTable<DefectListItemDto>
        {
            Title = "Зарегистрированные дефекты",
            Subtitle = DescribePeriod(query.DateFrom, query.DateTo),
            GeneratedBy = _currentUser.UserName,
            GeneratedAt = _clock.UtcNow,
            Rows = rows,
            Columns =
            [
                new("Дата", r => r.DetectedAt, ExportColumnFormat.Date, 0.9f),
                new("№ акта", r => r.InspectionNumber, RelativeWidth: 1.2f),
                new("Партия", r => r.BatchNumber, RelativeWidth: 1.2f),
                new("Изделие", r => r.ProductName, RelativeWidth: 1.8f),
                new("Участок", r => r.DepartmentName, RelativeWidth: 1.3f),
                new("Вид дефекта", r => r.DefectTypeName, RelativeWidth: 1.6f),
                new("Описание", r => r.Description, RelativeWidth: 2.5f),
                new("Критичность", r => Describe(r.Severity), RelativeWidth: 1.1f),
                new("Место", r => r.Location, RelativeWidth: 1.3f),
                new("Кол-во", r => r.AffectedQuantity, ExportColumnFormat.Integer, 0.7f),
                new("Статус", r => r.IsOpen ? "Открыт" : "Закрыт", RelativeWidth: 0.9f)
            ]
        };

        return await RenderAsync(table, format, "Дефекты", cancellationToken);
    }

    public async Task<ExportFile> ExportCorrectiveActionsAsync(
        CorrectiveActionQuery query,
        ExportFormat format,
        CancellationToken cancellationToken = default)
    {
        var rows = await _correctiveActions.GetForExportAsync(query, MaxExportRows, cancellationToken);

        var table = new ExportTable<CorrectiveActionListItemDto>
        {
            Title = "Корректирующие действия",
            Subtitle = DescribePeriod(query.DateFrom, query.DateTo),
            GeneratedBy = _currentUser.UserName,
            GeneratedAt = _clock.UtcNow,
            Rows = rows,
            Columns =
            [
                new("№", r => r.Id, ExportColumnFormat.Integer, 0.5f),
                new("Создано", r => r.CreatedAt, ExportColumnFormat.Date, 0.9f),
                new("Партия", r => r.BatchNumber, RelativeWidth: 1.2f),
                new("Изделие", r => r.ProductName, RelativeWidth: 1.6f),
                new("Участок", r => r.DepartmentName, RelativeWidth: 1.2f),
                new("Дефект", r => r.DefectDescription, RelativeWidth: 2.2f),
                new("Критичность", r => Describe(r.DefectSeverity), RelativeWidth: 1f),
                new("Мероприятие", r => r.Description, RelativeWidth: 2.2f),
                new("Ответственный", r => r.AssignedToName, RelativeWidth: 1.3f),
                new("Срок", r => r.DueDate, ExportColumnFormat.Date, 0.9f),
                new("Статус", r => Describe(r.Status), RelativeWidth: 1f),
                new("Просрочено", r => r.IsOverdue ? "Да" : "Нет", RelativeWidth: 0.8f)
            ]
        };

        return await RenderAsync(table, format, "Корректирующие_действия", cancellationToken);
    }

    public async Task<ExportFile> ExportDepartmentStatisticsAsync(
        ReportPeriodQuery query,
        ExportFormat format,
        CancellationToken cancellationToken = default)
    {
        var rows = await _reports.GetDepartmentStatisticsAsync(query, cancellationToken);

        var table = new ExportTable<DepartmentStatisticsDto>
        {
            Title = "Статистика по подразделениям",
            Subtitle = DescribePeriod(query.DateFrom, query.DateTo),
            GeneratedBy = _currentUser.UserName,
            GeneratedAt = _clock.UtcNow,
            Rows = rows,
            Columns =
            [
                new("Подразделение", r => r.DepartmentName, RelativeWidth: 2f),
                new("Проверок", r => r.InspectionCount, ExportColumnFormat.Integer),
                new("Проверено изделий", r => r.UnitsInspected, ExportColumnFormat.Integer),
                new("Забраковано", r => r.UnitsRejected, ExportColumnFormat.Integer),
                new("Процент брака", r => r.DefectRatePercent, ExportColumnFormat.Percent),
                new("Дефектов", r => r.DefectCount, ExportColumnFormat.Integer),
                new("Открытых дефектов", r => r.OpenDefectCount, ExportColumnFormat.Integer)
            ]
        };

        return await RenderAsync(table, format, "Статистика_по_подразделениям", cancellationToken);
    }

    public async Task<ExportFile> ExportTopDefectTypesAsync(
        ReportPeriodQuery query,
        ExportFormat format,
        CancellationToken cancellationToken = default)
    {
        var rows = await _reports.GetTopDefectTypesAsync(query, 50, cancellationToken);

        var table = new ExportTable<DefectTypeStatisticsDto>
        {
            Title = "Наиболее частые дефекты",
            Subtitle = DescribePeriod(query.DateFrom, query.DateTo),
            GeneratedBy = _currentUser.UserName,
            GeneratedAt = _clock.UtcNow,
            Rows = rows,
            Columns =
            [
                new("Код", r => r.DefectTypeCode, RelativeWidth: 0.8f),
                new("Вид дефекта", r => r.DefectTypeName, RelativeWidth: 2.5f),
                new("Количество", r => r.DefectCount, ExportColumnFormat.Integer),
                new("Изделий затронуто", r => r.AffectedUnits, ExportColumnFormat.Integer),
                new("Доля", r => r.SharePercent, ExportColumnFormat.Percent),
                new("Из них критических", r => r.CriticalCount, ExportColumnFormat.Integer)
            ]
        };

        return await RenderAsync(table, format, "Частые_дефекты", cancellationToken);
    }

    private async Task<ExportFile> RenderAsync<T>(
        ExportTable<T> table,
        ExportFormat format,
        string fileNamePrefix,
        CancellationToken cancellationToken)
    {
        var stamp = _clock.UtcNow.ToString("yyyyMMdd_HHmm");

        var file = format switch
        {
            ExportFormat.Excel => new ExportFile(
                _excel.Export(table),
                IExcelExportService.ContentType,
                $"{fileNamePrefix}_{stamp}.xlsx"),

            ExportFormat.Pdf => new ExportFile(
                _pdf.Export(table),
                IPdfExportService.ContentType,
                $"{fileNamePrefix}_{stamp}.pdf"),

            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Неизвестный формат экспорта.")
        };

        // Exports carry production data off the system; who took what, and when,
        // is worth recording.
        await _audit.LogAsync(
            AuditAction.Export,
            description: $"Экспорт отчёта «{table.Title}» в {format}. Строк: {table.Rows.Count}.",
            cancellationToken: cancellationToken);

        return file;
    }

    private static string DescribePeriod(DateTime? from, DateTime? to) => (from, to) switch
    {
        (null, null) => "За всё время",
        ({ } f, null) => $"С {f:dd.MM.yyyy}",
        (null, { } t) => $"По {t:dd.MM.yyyy}",
        ({ } f, { } t) => $"Период: {f:dd.MM.yyyy} — {t:dd.MM.yyyy}"
    };

    private static string Describe(InspectionOutcome outcome) => outcome switch
    {
        InspectionOutcome.Conforming => "Соответствует требованиям",
        InspectionOutcome.ReworkRequired => "Требуется доработка",
        InspectionOutcome.Defective => "Брак",
        _ => outcome.ToString()
    };

    private static string Describe(DefectSeverity severity) => severity switch
    {
        DefectSeverity.Minor => "Незначительный",
        DefectSeverity.Major => "Значительный",
        DefectSeverity.Critical => "Критический",
        _ => severity.ToString()
    };

    private static string Describe(CorrectiveActionStatus status) => status switch
    {
        CorrectiveActionStatus.New => "Новый",
        CorrectiveActionStatus.Assigned => "Назначен",
        CorrectiveActionStatus.InProgress => "В работе",
        CorrectiveActionStatus.Fixed => "Исправлен",
        CorrectiveActionStatus.Verified => "Проверен",
        CorrectiveActionStatus.Closed => "Закрыт",
        _ => status.ToString()
    };
}
