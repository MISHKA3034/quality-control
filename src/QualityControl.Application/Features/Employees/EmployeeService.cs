using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using QualityControl.Application.Common.Exceptions;
using QualityControl.Application.Common.Extensions;
using QualityControl.Application.Common.Interfaces;
using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.Employees.Dtos;
using QualityControl.Domain.Entities;
using QualityControl.Domain.Exceptions;

namespace QualityControl.Application.Features.Employees;

/// <inheritdoc cref="IEmployeeService"/>
public class EmployeeService : IEmployeeService
{
    private static readonly Dictionary<string, Expression<Func<Employee, object?>>> Sorts =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["personnelNumber"] = e => e.PersonnelNumber,
            ["lastName"] = e => e.LastName,
            ["position"] = e => e.Position,
            ["department"] = e => e.Department.Name
        };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public EmployeeService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public Task<PagedResult<EmployeeDto>> GetPagedAsync(
        EmployeeQuery query,
        CancellationToken cancellationToken = default)
    {
        var search = query.Search?.Trim();

        var source = _unitOfWork.Repository<Employee>()
            .Query()
            .WhereIf(query.DepartmentId is not null, e => e.DepartmentId == query.DepartmentId)
            .WhereIf(query.IsActive is not null, e => e.IsActive == query.IsActive)
            .WhereIf(
                !string.IsNullOrWhiteSpace(search),
                e => EF.Functions.Like(e.LastName, $"%{search}%")
                     || EF.Functions.Like(e.FirstName, $"%{search}%")
                     || EF.Functions.Like(e.PersonnelNumber, $"%{search}%")
                     || EF.Functions.Like(e.Position, $"%{search}%"))
            .ApplySort(query.SortBy, query.SortDescending, Sorts, e => e.LastName);

        return source.ToPagedResultAsync<Employee, EmployeeDto>(
            _mapper.ConfigurationProvider,
            query,
            cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeDto>> GetAllActiveAsync(
        int? departmentId = null,
        CancellationToken cancellationToken = default) =>
        await _unitOfWork.Repository<Employee>()
            .Query()
            .Where(e => e.IsActive)
            .WhereIf(departmentId is not null, e => e.DepartmentId == departmentId)
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .ProjectTo<EmployeeDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

    public async Task<EmployeeDto> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _unitOfWork.Repository<Employee>()
            .Query()
            .Where(e => e.Id == id)
            .ProjectTo<EmployeeDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("Сотрудник", id);

    public async Task<EmployeeDto> CreateAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var personnelNumber = request.PersonnelNumber.Trim();
        await EnsurePersonnelNumberIsUniqueAsync(personnelNumber, null, cancellationToken);
        await EnsureDepartmentExistsAsync(request.DepartmentId, cancellationToken);

        var employee = new Employee
        {
            PersonnelNumber = personnelNumber,
            LastName = request.LastName.Trim(),
            FirstName = request.FirstName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(request.MiddleName) ? null : request.MiddleName.Trim(),
            Position = request.Position.Trim(),
            DepartmentId = request.DepartmentId,
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            HiredOn = request.HiredOn,
            IsActive = true
        };

        await _unitOfWork.Repository<Employee>().AddAsync(employee, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(employee.Id, cancellationToken);
    }

    public async Task<EmployeeDto> UpdateAsync(
        int id,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var employee = await _unitOfWork.Repository<Employee>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Сотрудник", id);

        var personnelNumber = request.PersonnelNumber.Trim();
        await EnsurePersonnelNumberIsUniqueAsync(personnelNumber, id, cancellationToken);
        await EnsureDepartmentExistsAsync(request.DepartmentId, cancellationToken);

        employee.PersonnelNumber = personnelNumber;
        employee.LastName = request.LastName.Trim();
        employee.FirstName = request.FirstName.Trim();
        employee.MiddleName = string.IsNullOrWhiteSpace(request.MiddleName) ? null : request.MiddleName.Trim();
        employee.Position = request.Position.Trim();
        employee.DepartmentId = request.DepartmentId;
        employee.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        employee.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        employee.HiredOn = request.HiredOn;
        employee.DismissedOn = request.DismissedOn;
        employee.IsActive = request.IsActive;

        _unitOfWork.Repository<Employee>().Update(employee);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var employee = await _unitOfWork.Repository<Employee>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Сотрудник", id);

        // An employee who signed an act or ran a batch is part of the quality record.
        // Dismissing them (IsActive = false) is the correct way to retire the record.
        var isReferenced =
            await _unitOfWork.Repository<QualityInspection>()
                .ExistsAsync(i => i.InspectorId == id, cancellationToken)
            || await _unitOfWork.Repository<ProductionBatch>()
                .ExistsAsync(b => b.ForemanId == id, cancellationToken)
            || await _unitOfWork.Repository<CorrectiveAction>()
                .ExistsAsync(a => a.AssignedToId == id, cancellationToken);

        if (isReferenced)
        {
            throw new DomainException(
                $"Сотрудник «{employee.FullName}» указан в документах качества и не может быть удалён. "
                + "Отметьте его как уволенного.");
        }

        if (await _unitOfWork.Repository<User>().ExistsAsync(u => u.EmployeeId == id, cancellationToken))
        {
            throw new DomainException(
                $"К сотруднику «{employee.FullName}» привязана учётная запись. Удалите её перед удалением сотрудника.");
        }

        _unitOfWork.Repository<Employee>().Remove(employee);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsurePersonnelNumberIsUniqueAsync(
        string personnelNumber,
        int? excludingId,
        CancellationToken cancellationToken)
    {
        var taken = await _unitOfWork.Repository<Employee>()
            .ExistsAsync(
                e => e.PersonnelNumber == personnelNumber && (excludingId == null || e.Id != excludingId),
                cancellationToken);

        if (taken)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(CreateEmployeeRequest.PersonnelNumber)] =
                    [$"Сотрудник с табельным номером «{personnelNumber}» уже существует."]
            });
        }
    }

    private async Task EnsureDepartmentExistsAsync(int departmentId, CancellationToken cancellationToken)
    {
        if (!await _unitOfWork.Repository<Department>().ExistsAsync(d => d.Id == departmentId, cancellationToken))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(CreateEmployeeRequest.DepartmentId)] = ["Указанное подразделение не найдено."]
            });
        }
    }
}
