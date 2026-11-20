using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using QualityControl.Application.Common.Exceptions;
using QualityControl.Application.Common.Extensions;
using QualityControl.Application.Common.Interfaces;
using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.Departments.Dtos;
using QualityControl.Domain.Entities;
using QualityControl.Domain.Exceptions;

namespace QualityControl.Application.Features.Departments;

/// <inheritdoc cref="IDepartmentService"/>
public class DepartmentService : IDepartmentService
{
    private static readonly Dictionary<string, Expression<Func<Department, object?>>> Sorts =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["code"] = d => d.Code,
            ["name"] = d => d.Name
        };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public DepartmentService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public Task<PagedResult<DepartmentDto>> GetPagedAsync(
        DepartmentQuery query,
        CancellationToken cancellationToken = default)
    {
        var search = query.Search?.Trim();

        var source = _unitOfWork.Repository<Department>()
            .Query()
            .WhereIf(query.IsActive is not null, d => d.IsActive == query.IsActive)
            .WhereIf(query.ParentDepartmentId is not null, d => d.ParentDepartmentId == query.ParentDepartmentId)
            .WhereIf(
                !string.IsNullOrWhiteSpace(search),
                d => EF.Functions.Like(d.Name, $"%{search}%") || EF.Functions.Like(d.Code, $"%{search}%"))
            .ApplySort(query.SortBy, query.SortDescending, Sorts, d => d.Name);

        return source.ToPagedResultAsync<Department, DepartmentDto>(
            _mapper.ConfigurationProvider,
            query,
            cancellationToken);
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _unitOfWork.Repository<Department>()
            .Query()
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .ProjectTo<DepartmentDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

    public async Task<DepartmentDto> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _unitOfWork.Repository<Department>()
            .Query()
            .Where(d => d.Id == id)
            .ProjectTo<DepartmentDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("Подразделение", id);

    public async Task<DepartmentDto> CreateAsync(
        CreateDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim();
        await EnsureCodeIsUniqueAsync(code, null, cancellationToken);
        await EnsureParentExistsAsync(request.ParentDepartmentId, cancellationToken);
        await EnsureManagerExistsAsync(request.ManagerId, cancellationToken);

        var department = new Department
        {
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            ParentDepartmentId = request.ParentDepartmentId,
            ManagerId = request.ManagerId,
            IsActive = true
        };

        await _unitOfWork.Repository<Department>().AddAsync(department, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(department.Id, cancellationToken);
    }

    public async Task<DepartmentDto> UpdateAsync(
        int id,
        UpdateDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var department = await _unitOfWork.Repository<Department>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Подразделение", id);

        var code = request.Code.Trim();
        await EnsureCodeIsUniqueAsync(code, id, cancellationToken);
        await EnsureParentExistsAsync(request.ParentDepartmentId, cancellationToken);
        await EnsureManagerExistsAsync(request.ManagerId, cancellationToken);

        if (request.ParentDepartmentId == id)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(UpdateDepartmentRequest.ParentDepartmentId)] =
                    ["Подразделение не может быть вложено само в себя."]
            });
        }

        department.Code = code;
        department.Name = request.Name.Trim();
        department.Description = request.Description?.Trim();
        department.ParentDepartmentId = request.ParentDepartmentId;
        department.ManagerId = request.ManagerId;
        department.IsActive = request.IsActive;

        _unitOfWork.Repository<Department>().Update(department);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var department = await _unitOfWork.Repository<Department>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Подразделение", id);

        if (await _unitOfWork.Repository<Employee>().ExistsAsync(e => e.DepartmentId == id, cancellationToken))
        {
            throw new DomainException(
                $"В подразделении «{department.Name}» числятся сотрудники. Переведите их перед удалением.");
        }

        if (await _unitOfWork.Repository<ProductionBatch>()
                .ExistsAsync(b => b.DepartmentId == id, cancellationToken))
        {
            throw new DomainException(
                $"На подразделение «{department.Name}» оформлены производственные партии. "
                + "Отключите подразделение вместо удаления.");
        }

        if (await _unitOfWork.Repository<Department>()
                .ExistsAsync(d => d.ParentDepartmentId == id, cancellationToken))
        {
            throw new DomainException($"Подразделение «{department.Name}» содержит вложенные подразделения.");
        }

        _unitOfWork.Repository<Department>().Remove(department);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCodeIsUniqueAsync(string code, int? excludingId, CancellationToken cancellationToken)
    {
        var taken = await _unitOfWork.Repository<Department>()
            .ExistsAsync(d => d.Code == code && (excludingId == null || d.Id != excludingId), cancellationToken);

        if (taken)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(CreateDepartmentRequest.Code)] = [$"Подразделение с кодом «{code}» уже существует."]
            });
        }
    }

    private async Task EnsureParentExistsAsync(int? parentId, CancellationToken cancellationToken)
    {
        if (parentId is not null
            && !await _unitOfWork.Repository<Department>().ExistsAsync(d => d.Id == parentId, cancellationToken))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(CreateDepartmentRequest.ParentDepartmentId)] = ["Родительское подразделение не найдено."]
            });
        }
    }

    private async Task EnsureManagerExistsAsync(int? managerId, CancellationToken cancellationToken)
    {
        if (managerId is not null
            && !await _unitOfWork.Repository<Employee>().ExistsAsync(e => e.Id == managerId, cancellationToken))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(CreateDepartmentRequest.ManagerId)] = ["Указанный руководитель не найден."]
            });
        }
    }
}
