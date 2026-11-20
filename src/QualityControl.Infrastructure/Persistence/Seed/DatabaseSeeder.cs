using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QualityControl.Application.Common.Interfaces;
using QualityControl.Domain.Entities;
using QualityControl.Domain.Enums;

namespace QualityControl.Infrastructure.Persistence.Seed;

/// <summary>
/// Brings a fresh database up to a usable state: reference data always, plus a
/// demo dataset when explicitly enabled.
/// </summary>
public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Seeds reference data and, if <c>Seed:DemoData</c> is true, a demo dataset.
    /// Every step is idempotent — running it on a populated database changes nothing.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync(cancellationToken);
        await SeedDefectTypesAsync(cancellationToken);
        await SeedAdministratorAsync(cancellationToken);

        if (_configuration.GetValue<bool>("Seed:DemoData"))
        {
            await SeedDemoDataAsync(cancellationToken);
        }
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        if (await _context.Roles.AnyAsync(cancellationToken))
        {
            return;
        }

        _context.Roles.AddRange(
            new Role
            {
                Name = RoleNames.Administrator,
                DisplayName = "Администратор",
                Description = "Полный доступ, управление пользователями и справочниками."
            },
            new Role
            {
                Name = RoleNames.QualityInspector,
                DisplayName = "Контролёр ОТК",
                Description = "Оформление актов проверки, регистрация дефектов, приёмка устранения."
            },
            new Role
            {
                Name = RoleNames.WorkshopManager,
                DisplayName = "Начальник участка",
                Description = "Партии своего участка, исполнение корректирующих действий."
            },
            new Role
            {
                Name = RoleNames.ProductionManager,
                DisplayName = "Руководитель производства",
                Description = "Аналитика и отчётность по всем подразделениям."
            });

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Справочник ролей заполнен.");
    }

    private async Task SeedDefectTypesAsync(CancellationToken cancellationToken)
    {
        if (await _context.DefectTypes.AnyAsync(cancellationToken))
        {
            return;
        }

        _context.DefectTypes.AddRange(
            Type("ДФ-01", "Несоответствие размеров", DefectSeverity.Major,
                "Отклонение фактического размера от требований чертежа."),
            Type("ДФ-02", "Непровар сварного шва", DefectSeverity.Critical,
                "Отсутствие сплавления по сечению шва."),
            Type("ДФ-03", "Подрез сварного шва", DefectSeverity.Major,
                "Углубление по линии сплавления шва с основным металлом."),
            Type("ДФ-04", "Пора в сварном шве", DefectSeverity.Minor,
                "Газовая полость в металле шва."),
            Type("ДФ-05", "Дефект лакокрасочного покрытия", DefectSeverity.Minor,
                "Потёки, непрокрас, отслоение покрытия."),
            Type("ДФ-06", "Коррозия металла", DefectSeverity.Major,
                "Следы коррозии на поверхности детали."),
            Type("ДФ-07", "Механическое повреждение", DefectSeverity.Major,
                "Забоины, вмятины, царапины, полученные при обработке или транспортировке."),
            Type("ДФ-08", "Трещина основного металла", DefectSeverity.Critical,
                "Трещина в теле детали или металлоконструкции."),
            Type("ДФ-09", "Несоответствие шероховатости", DefectSeverity.Minor,
                "Шероховатость поверхности не соответствует требованиям."),
            Type("ДФ-10", "Ошибка сборки", DefectSeverity.Critical,
                "Неверная установка, некомплект или отсутствие крепежа."));

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Справочник видов дефектов заполнен.");

        static DefectType Type(string code, string name, DefectSeverity severity, string description) => new()
        {
            Code = code,
            Name = name,
            DefaultSeverity = severity,
            Description = description,
            IsActive = true
        };
    }

    /// <summary>
    /// Creates the first administrator so a fresh installation can be signed into.
    /// </summary>
    private async Task SeedAdministratorAsync(CancellationToken cancellationToken)
    {
        if (await _context.Users.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            return;
        }

        var adminRole = await _context.Roles
            .FirstAsync(r => r.Name == RoleNames.Administrator, cancellationToken);

        var password = _configuration["Seed:AdministratorPassword"];

        // No fallback default. A hard-coded initial password is a published
        // credential the moment this repository is shared, and the one account
        // that gets it is the one that can do everything.
        if (string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning(
                "Учётная запись администратора не создана: не задан Seed:AdministratorPassword. "
                + "Задайте его через user-secrets или переменную окружения "
                + "(Seed__AdministratorPassword) и перезапустите приложение.");

            return;
        }

        _context.Users.Add(new User
        {
            UserName = _configuration["Seed:AdministratorUserName"] ?? "admin",
            Email = _configuration["Seed:AdministratorEmail"] ?? "admin@amkodor-pinsk.by",
            PasswordHash = _passwordHasher.Hash(password),
            RoleId = adminRole.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Создана учётная запись администратора. Смените пароль после первого входа.");
    }

    private async Task SeedDemoDataAsync(CancellationToken cancellationToken)
    {
        if (await _context.Departments.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Демонстрационные данные пропущены: подразделения уже существуют.");
            return;
        }

        _logger.LogInformation("Заполнение демонстрационных данных...");

        var departments = await SeedDepartmentsAsync(cancellationToken);
        var employees = await SeedEmployeesAsync(departments, cancellationToken);
        await SeedDemoUsersAsync(employees, cancellationToken);
        var products = await SeedProductsAsync(cancellationToken);
        await SeedBatchesAndInspectionsAsync(products, departments, employees, cancellationToken);

        _logger.LogInformation("Демонстрационные данные загружены.");
    }

    private async Task<Dictionary<string, Department>> SeedDepartmentsAsync(CancellationToken cancellationToken)
    {
        var departments = new List<Department>
        {
            new() { Code = "ОТК", Name = "Отдел технического контроля", IsActive = true },
            new() { Code = "ЦМК", Name = "Цех металлоконструкций", IsActive = true },
            new() { Code = "МЦ", Name = "Механический цех", IsActive = true },
            new() { Code = "СЦ", Name = "Сборочный цех", IsActive = true },
            new() { Code = "ЦОП", Name = "Цех окраски и покрытий", IsActive = true }
        };

        _context.Departments.AddRange(departments);
        await _context.SaveChangesAsync(cancellationToken);

        return departments.ToDictionary(d => d.Code);
    }

    private async Task<Dictionary<string, Employee>> SeedEmployeesAsync(
        Dictionary<string, Department> departments,
        CancellationToken cancellationToken)
    {
        var employees = new List<Employee>
        {
            Emp("1001", "Ковалёв", "Сергей", "Петрович", "Начальник ОТК", departments["ОТК"]),
            Emp("1002", "Марчук", "Ирина", "Владимировна", "Контролёр ОТК", departments["ОТК"]),
            Emp("1003", "Дорошевич", "Андрей", "Николаевич", "Контролёр ОТК", departments["ОТК"]),
            Emp("2001", "Пашкевич", "Виктор", "Иванович", "Мастер участка", departments["ЦМК"]),
            Emp("2002", "Сидорук", "Олег", "Михайлович", "Сварщик", departments["ЦМК"]),
            Emp("3001", "Гриневич", "Дмитрий", "Александрович", "Мастер участка", departments["МЦ"]),
            Emp("3002", "Лукашевич", "Павел", "Сергеевич", "Токарь", departments["МЦ"]),
            Emp("4001", "Романчук", "Наталья", "Петровна", "Мастер участка", departments["СЦ"]),
            Emp("5001", "Савчук", "Алексей", "Викторович", "Мастер участка", departments["ЦОП"]),
            Emp("9001", "Жук", "Тамара", "Ивановна", "Директор по производству", departments["ОТК"])
        };

        _context.Employees.AddRange(employees);
        await _context.SaveChangesAsync(cancellationToken);

        departments["ОТК"].ManagerId = employees[0].Id;
        departments["ЦМК"].ManagerId = employees[3].Id;
        departments["МЦ"].ManagerId = employees[5].Id;
        departments["СЦ"].ManagerId = employees[7].Id;
        departments["ЦОП"].ManagerId = employees[8].Id;

        await _context.SaveChangesAsync(cancellationToken);

        return employees.ToDictionary(e => e.PersonnelNumber);

        Employee Emp(string number, string last, string first, string middle, string position, Department dept) =>
            new()
            {
                PersonnelNumber = number,
                LastName = last,
                FirstName = first,
                MiddleName = middle,
                Position = position,
                DepartmentId = dept.Id,
                Department = dept,
                Email = $"{number}@amkodor-pinsk.by",
                HiredOn = new DateTime(2020, 1, 15),
                IsActive = true
            };
    }

    private async Task SeedDemoUsersAsync(
        Dictionary<string, Employee> employees,
        CancellationToken cancellationToken)
    {
        var password = _configuration["Seed:DemoUserPassword"];

        if (string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning(
                "Демонстрационные пользователи не созданы: не задан Seed:DemoUserPassword.");
            return;
        }

        var roles = await _context.Roles.ToDictionaryAsync(r => r.Name, cancellationToken);
        var hash = _passwordHasher.Hash(password);

        _context.Users.AddRange(
            User("inspector", "1002", RoleNames.QualityInspector),
            User("inspector2", "1003", RoleNames.QualityInspector),
            User("master.cmk", "2001", RoleNames.WorkshopManager),
            User("master.mc", "3001", RoleNames.WorkshopManager),
            User("production", "9001", RoleNames.ProductionManager));

        await _context.SaveChangesAsync(cancellationToken);

        User User(string userName, string personnelNumber, string roleName) => new()
        {
            UserName = userName,
            Email = $"{userName}@amkodor-pinsk.by",
            PasswordHash = hash,
            RoleId = roles[roleName].Id,
            EmployeeId = employees[personnelNumber].Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task<List<Product>> SeedProductsAsync(CancellationToken cancellationToken)
    {
        var categories = new List<ProductCategory>
        {
            new() { Code = "КАТ-01", Name = "Детали механической обработки" },
            new() { Code = "КАТ-02", Name = "Металлоконструкции" },
            new() { Code = "КАТ-03", Name = "Узлы в сборе" }
        };

        _context.ProductCategories.AddRange(categories);
        await _context.SaveChangesAsync(cancellationToken);

        var products = new List<Product>
        {
            Prod("АМ-3416.02.05.100", "Вал приводной", categories[0], "АМ-3416.02.05.100"),
            Prod("АМ-3416.02.05.120", "Втулка опорная", categories[0], "АМ-3416.02.05.120"),
            Prod("АМ-3416.02.06.010", "Палец шарнирный", categories[0], "АМ-3416.02.06.010"),
            Prod("АМ-3416.10.01.000", "Рама основная", categories[1], "АМ-3416.10.01.000"),
            Prod("АМ-3416.10.02.000", "Стрела погрузчика", categories[1], "АМ-3416.10.02.000"),
            Prod("АМ-3416.10.03.000", "Отвал снегоуборочный", categories[1], "АМ-3416.10.03.000"),
            Prod("АМ-3416.20.00.000", "Гидроцилиндр подъёма", categories[2], "АМ-3416.20.00.000"),
            Prod("АМ-3416.30.00.000", "Узел рулевого управления", categories[2], "АМ-3416.30.00.000")
        };

        _context.Products.AddRange(products);
        await _context.SaveChangesAsync(cancellationToken);

        return products;

        Product Prod(string code, string name, ProductCategory category, string drawing) => new()
        {
            Code = code,
            Name = name,
            CategoryId = category.Id,
            DrawingNumber = drawing,
            UnitOfMeasure = "шт.",
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Generates a year of batches, inspections, defects and corrective actions so
    /// the dashboard has a trend to draw on a fresh install.
    /// </summary>
    private async Task SeedBatchesAndInspectionsAsync(
        List<Product> products,
        Dictionary<string, Department> departments,
        Dictionary<string, Employee> employees,
        CancellationToken cancellationToken)
    {
        // Fixed seed: the demo dataset must look the same on every machine, or
        // screenshots and bug reports stop matching.
        var random = new Random(20260717);

        var defectTypes = await _context.DefectTypes.ToListAsync(cancellationToken);
        var inspectors = new[] { employees["1002"], employees["1003"] };

        var productionDepartments = new[]
        {
            departments["ЦМК"], departments["МЦ"], departments["СЦ"], departments["ЦОП"]
        };

        var foremen = new Dictionary<int, Employee>
        {
            [departments["ЦМК"].Id] = employees["2001"],
            [departments["МЦ"].Id] = employees["3001"],
            [departments["СЦ"].Id] = employees["4001"],
            [departments["ЦОП"].Id] = employees["5001"]
        };

        var batches = new List<ProductionBatch>();
        var inspections = new List<QualityInspection>();
        var start = DateTime.UtcNow.Date.AddMonths(-11);
        var batchOrdinal = 1;
        var actOrdinal = 1;

        for (var monthOffset = 0; monthOffset < 12; monthOffset++)
        {
            var month = start.AddMonths(monthOffset);
            var batchesThisMonth = random.Next(6, 11);

            for (var i = 0; i < batchesThisMonth; i++)
            {
                var manufactureDate = month.AddDays(random.Next(0, 27));
                var department = productionDepartments[random.Next(productionDepartments.Length)];
                var product = products[random.Next(products.Count)];
                var quantity = random.Next(10, 200);

                var batch = new ProductionBatch
                {
                    BatchNumber = $"П-{manufactureDate:yyyyMM}-{batchOrdinal++:D3}",
                    ProductId = product.Id,
                    ManufactureDate = manufactureDate,
                    DepartmentId = department.Id,
                    ForemanId = foremen[department.Id].Id,
                    Quantity = quantity,
                    Status = BatchStatus.InProduction,
                    CreatedAt = manufactureDate
                };

                batches.Add(batch);

                // Roughly one batch in eight is still open, so the board is not
                // uniformly "everything inspected".
                if (random.Next(8) == 0)
                {
                    batch.Status = BatchStatus.AwaitingInspection;
                    continue;
                }

                var inspectionDate = manufactureDate.AddDays(random.Next(1, 4));
                var inspector = inspectors[random.Next(inspectors.Length)];

                // ~72% clean, ~20% rework, ~8% rejected — plausible for a plant that
                // is working properly but not perfectly.
                var roll = random.Next(100);

                var outcome = roll switch
                {
                    < 72 => InspectionOutcome.Conforming,
                    < 92 => InspectionOutcome.ReworkRequired,
                    _ => InspectionOutcome.Defective
                };

                var inspected = Math.Max(5, quantity / random.Next(2, 5));

                var rejected = outcome switch
                {
                    InspectionOutcome.Conforming => 0,
                    InspectionOutcome.ReworkRequired => Math.Max(1, inspected / random.Next(8, 15)),
                    _ => Math.Max(1, inspected / random.Next(2, 4))
                };

                var inspection = new QualityInspection
                {
                    InspectionNumber = $"АКТ-{inspectionDate.Year}-{actOrdinal++:D5}",
                    ProductionBatch = batch,
                    InspectorId = inspector.Id,
                    InspectionDate = inspectionDate,
                    Outcome = outcome,
                    QuantityInspected = inspected,
                    QuantityAccepted = inspected - rejected,
                    QuantityRejected = rejected,
                    Comment = outcome == InspectionOutcome.Conforming
                        ? "Партия соответствует требованиям КД."
                        : null,
                    CreatedAt = inspectionDate
                };

                inspection.Results.Add(new InspectionResult
                {
                    ParameterName = "Соответствие геометрии чертежу",
                    ExpectedValue = "По КД",
                    ActualValue = outcome == InspectionOutcome.Conforming ? "Соответствует" : "Отклонение",
                    Outcome = outcome,
                    SortOrder = 1
                });

                inspection.Results.Add(new InspectionResult
                {
                    ParameterName = "Качество поверхности",
                    ExpectedValue = "Без видимых дефектов",
                    ActualValue = outcome == InspectionOutcome.Conforming ? "Соответствует" : "Замечания",
                    Outcome = outcome,
                    SortOrder = 2
                });

                if (outcome != InspectionOutcome.Conforming)
                {
                    var defectCount = random.Next(1, 3);

                    for (var d = 0; d < defectCount; d++)
                    {
                        var defectType = defectTypes[random.Next(defectTypes.Count)];

                        inspection.Defects.Add(new Defect
                        {
                            DefectTypeId = defectType.Id,
                            Description = $"{defectType.Name} — обнаружено при контроле партии {batch.BatchNumber}.",
                            Severity = defectType.DefaultSeverity,
                            Location = RandomLocation(random),
                            AffectedQuantity = Math.Max(1, rejected / defectCount),
                            DetectedAt = inspectionDate,
                            CreatedAt = inspectionDate
                        });
                    }
                }

                batch.Status = outcome switch
                {
                    InspectionOutcome.Conforming => BatchStatus.Accepted,
                    InspectionOutcome.ReworkRequired => BatchStatus.ReworkRequired,
                    _ => BatchStatus.Rejected
                };

                inspections.Add(inspection);
            }
        }

        _context.ProductionBatches.AddRange(batches);
        _context.QualityInspections.AddRange(inspections);
        await _context.SaveChangesAsync(cancellationToken);

        await SeedCorrectiveActionsAsync(employees, random, cancellationToken);
    }

    private async Task SeedCorrectiveActionsAsync(
        Dictionary<string, Employee> employees,
        Random random,
        CancellationToken cancellationToken)
    {
        var adminUser = await _context.Users
            .OrderBy(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (adminUser is null)
        {
            _logger.LogWarning(
                "Корректирующие действия не созданы: отсутствует пользователь для записи истории.");
            return;
        }

        var defects = await _context.Defects
            .Include(d => d.QualityInspection)
            .OrderBy(d => d.Id)
            .ToListAsync(cancellationToken);

        var assignees = new[] { employees["2002"], employees["3002"], employees["2001"], employees["3001"] };
        var inspectorEmployeeId = employees["1002"].Id;
        var actions = new List<CorrectiveAction>();

        foreach (var defect in defects)
        {
            // Not every defect gets one: some are still awaiting a decision, which is
            // what makes the "open defects" counter meaningful.
            if (random.Next(10) < 2)
            {
                continue;
            }

            var created = defect.DetectedAt.AddDays(1);
            var assignee = assignees[random.Next(assignees.Length)];

            var action = new CorrectiveAction
            {
                DefectId = defect.Id,
                Description = $"Устранить: {defect.Description}",
                DueDate = created.AddDays(random.Next(5, 21)),
                CreatedAt = created
            };

            action.RecordCreation(adminUser.Id, created);
            action.AssignTo(assignee.Id, adminUser.Id, created, "Назначен ответственный.");

            // Older defects are mostly resolved; recent ones are still moving. This
            // is what gives the dashboard a believable mix of states.
            var age = (DateTime.UtcNow - created).TotalDays;
            var progress = age > 60 ? random.Next(100) : random.Next(60);

            if (progress > 10)
            {
                action.ChangeStatus(
                    CorrectiveActionStatus.InProgress,
                    adminUser.Id,
                    created.AddDays(1),
                    "Работы начаты.");
            }

            if (progress > 35)
            {
                action.ChangeStatus(
                    CorrectiveActionStatus.Fixed,
                    adminUser.Id,
                    created.AddDays(3),
                    "Дефект устранён.");

                action.ResolutionNotes = "Выполнена доработка согласно технологии.";
            }

            if (progress > 55)
            {
                action.ChangeStatus(
                    CorrectiveActionStatus.Verified,
                    adminUser.Id,
                    created.AddDays(4),
                    "Устранение принято ОТК.",
                    inspectorEmployeeId);
            }

            if (progress > 70)
            {
                action.ChangeStatus(
                    CorrectiveActionStatus.Closed,
                    adminUser.Id,
                    created.AddDays(5),
                    "Замечание закрыто.");
            }

            actions.Add(action);
        }

        _context.CorrectiveActions.AddRange(actions);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string RandomLocation(Random random)
    {
        string[] locations =
        [
            "Сварной шов Ш-1",
            "Сварной шов Ш-4",
            "Посадочная поверхность Ø80",
            "Отверстие Ø20H7",
            "Наружная поверхность",
            "Узел крепления левый",
            "Узел крепления правый",
            "Кромка листа"
        ];

        return locations[random.Next(locations.Length)];
    }
}
