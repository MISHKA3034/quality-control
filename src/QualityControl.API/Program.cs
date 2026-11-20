using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QualityControl.API.Filters;
using QualityControl.API.Middleware;
using QualityControl.API.Services;
using QualityControl.Application;
using QualityControl.Application.Common.Interfaces;
using QualityControl.Infrastructure;
using QualityControl.Infrastructure.Persistence;
using QualityControl.Infrastructure.Persistence.Seed;
using QualityControl.Infrastructure.Security;
using QuestPDF.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configured from appsettings so log levels and sinks can change on a running
// installation without a rebuild.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// QuestPDF refuses to render until a licence is declared. Community is valid for
// organisations under the vendor's revenue threshold — see "Лицензии" in README.
QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddControllers(options =>
    {
        // Global: registering a validator is enough to have it enforced, and no
        // controller can forget to call it.
        options.Filters.Add<ValidationFilter>();
    })
    .AddJsonOptions(options =>
    {
        // Enums travel as names. Numbers would make the API unreadable and would
        // silently change meaning if a member were ever renumbered.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Отсутствует секция конфигурации Jwt.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,

            // Default is five minutes, which would keep an expired token working
            // well past its stated expiry.
            ClockSkew = TimeSpan.Zero,

            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name
        };

        // Keeps the claim names exactly as TokenService issued them, instead of
        // remapping them to legacy SOAP-era URIs.
        options.MapInboundClaims = false;
    });

builder.Services.AddAuthorization();

const string CorsPolicy = "SpaCors";

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173"];

        // Named origins only, never AllowAnyOrigin: this API is driven with a
        // bearer token, and a wildcard would let any site call it from a browser.
        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()

            // Lets the SPA read the download name from a report response.
            .WithExposedHeaders("Content-Disposition");
    });
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "АСКК «Амкодор-Пинск» — API",
        Version = "v1",
        Description =
            "Автоматизированная система контроля качества продукции.\n\n"
            + "Аутентификация: JWT Bearer. Получите токен через POST /api/auth/login, "
            + "затем нажмите «Authorize» и вставьте его."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Вставьте только сам токен — префикс «Bearer» добавляется автоматически."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Surfaces the controllers' Russian XML docs in Swagger UI.
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database");

var app = builder.Build();

// First in the pipeline: it must see exceptions thrown by everything after it.
app.UseExceptionHandling();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "АСКК — API v1");
        options.DocumentTitle = "АСКК «Амкодор-Пинск» — API";
    });
}
else
{
    // Not in development: HSTS pins the browser to HTTPS for months, and doing
    // that to localhost breaks later plain-HTTP work on the same machine.
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors(CorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

await InitialiseDatabaseAsync(app);

app.Run();

// Applies migrations and seeds reference data at startup.
//
// Right for development and for a single-server on-premises install. Several
// instances behind a balancer would race here, so it is gated on configuration:
// set Database:MigrateOnStartup to false and run `dotnet ef database update` as
// a deployment step instead.
static async Task InitialiseDatabaseAsync(WebApplication app)
{
    if (!app.Configuration.GetValue("Database:MigrateOnStartup", defaultValue: true))
    {
        return;
    }

    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        logger.LogInformation("Применение миграций базы данных...");
        await context.Database.MigrateAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();

        logger.LogInformation("База данных готова к работе.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Не удалось подготовить базу данных. Приложение остановлено.");

        // Deliberately fatal. Serving requests against an un-migrated schema would
        // fail confusingly for every user, instead of once and loudly here.
        throw;
    }
}
