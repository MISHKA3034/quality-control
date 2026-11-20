using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QualityControl.Application.Common.Interfaces;
using QualityControl.Infrastructure.Persistence;
using QualityControl.Infrastructure.Persistence.Repositories;
using QualityControl.Infrastructure.Persistence.Seed;
using QualityControl.Infrastructure.Security;
using QualityControl.Infrastructure.Services;
using QualityControl.Infrastructure.Services.Export;

namespace QualityControl.Infrastructure;

/// <summary>
/// Wires up the concrete implementations behind the Application layer's abstractions.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Не задана строка подключения ConnectionStrings:DefaultConnection.");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);

                // Retries transient faults (deadlocks, brief connectivity loss)
                // instead of surfacing them to the user as a 500.
                sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);

                sql.CommandTimeout(60);
            });
        });

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))

            // Fails at startup, not on the first login attempt: a missing signing key
            // must be a deployment error, not a runtime surprise.
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName));

        services.AddOptions<PdfExportOptions>()
            .Bind(configuration.GetSection(PdfExportOptions.SectionName));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<DatabaseSeeder>();

        // Stateless and cheap; no per-request state to hold.
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IExcelExportService, ExcelExportService>();
        services.AddSingleton<IPdfExportService, PdfExportService>();

        return services;
    }
}
