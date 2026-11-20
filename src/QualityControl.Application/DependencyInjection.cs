using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QualityControl.Application.Common.Options;
using QualityControl.Application.Features.Auth;
using QualityControl.Application.Features.AuditLogs;
using QualityControl.Application.Features.CorrectiveActions;
using QualityControl.Application.Features.Defects;
using QualityControl.Application.Features.DefectTypes;
using QualityControl.Application.Features.Departments;
using QualityControl.Application.Features.Employees;
using QualityControl.Application.Features.ProductCategories;
using QualityControl.Application.Features.ProductionBatches;
using QualityControl.Application.Features.Products;
using QualityControl.Application.Features.QualityInspections;
using QualityControl.Application.Features.Reports;
using QualityControl.Application.Features.Users;

namespace QualityControl.Application;

/// <summary>
/// Registers everything the Application layer owns. Infrastructure supplies the
/// implementations of the interfaces under Common/Interfaces.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // AuthOptions carries a working default for every setting, so an absent
        // section is valid and there is nothing to validate.
        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName));

        services.AddOptions<FileStorageOptions>()
            .Bind(configuration.GetSection(FileStorageOptions.SectionName))

            // The upload allowlists have no code-side defaults (see FileStorageOptions
            // for why), so an empty list would silently reject every photo. Catch that
            // at startup rather than at the first upload.
            .Validate(
                o => o.AllowedPhotoContentTypes.Length > 0,
                "Не задан FileStorage:AllowedPhotoContentTypes — загрузка фотографий будет невозможна.")
            .Validate(
                o => o.AllowedPhotoExtensions.Length > 0,
                "Не задан FileStorage:AllowedPhotoExtensions — загрузка фотографий будет невозможна.")
            .Validate(
                o => o.MaxPhotoSizeBytes > 0,
                "FileStorage:MaxPhotoSizeBytes должен быть больше нуля.")
            .ValidateOnStart();

        // Picks up every Profile in this assembly, so a new feature's mappings are
        // registered by existing here rather than by editing this file.
        services.AddAutoMapper(cfg => cfg.AddMaps(assembly));

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // Scoped: each carries a unit of work bound to the request's DbContext.
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IProductCategoryService, ProductCategoryService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IDefectTypeService, DefectTypeService>();
        services.AddScoped<IProductionBatchService, ProductionBatchService>();
        services.AddScoped<IQualityInspectionService, QualityInspectionService>();
        services.AddScoped<IDefectService, DefectService>();
        services.AddScoped<ICorrectiveActionService, CorrectiveActionService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IReportExportService, ReportExportService>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        return services;
    }
}
