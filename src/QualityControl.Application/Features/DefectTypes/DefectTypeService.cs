using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using QualityControl.Application.Common.Exceptions;
using QualityControl.Application.Common.Extensions;
using QualityControl.Application.Common.Interfaces;
using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.DefectTypes.Dtos;
using QualityControl.Domain.Entities;
using QualityControl.Domain.Exceptions;

namespace QualityControl.Application.Features.DefectTypes;

/// <inheritdoc cref="IDefectTypeService"/>
public class DefectTypeService : IDefectTypeService
{
    private static readonly Dictionary<string, Expression<Func<DefectType, object?>>> Sorts =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["code"] = t => t.Code,
            ["name"] = t => t.Name,
            ["defaultSeverity"] = t => t.DefaultSeverity
        };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public DefectTypeService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public Task<PagedResult<DefectTypeDto>> GetPagedAsync(
        DefectTypeQuery query,
        CancellationToken cancellationToken = default)
    {
        var search = query.Search?.Trim();

        var source = _unitOfWork.Repository<DefectType>()
            .Query()
            .WhereIf(query.IsActive is not null, t => t.IsActive == query.IsActive)
            .WhereIf(query.DefaultSeverity is not null, t => t.DefaultSeverity == query.DefaultSeverity)
            .WhereIf(
                !string.IsNullOrWhiteSpace(search),
                t => EF.Functions.Like(t.Name, $"%{search}%") || EF.Functions.Like(t.Code, $"%{search}%"))
            .ApplySort(query.SortBy, query.SortDescending, Sorts, t => t.Code);

        return source.ToPagedResultAsync<DefectType, DefectTypeDto>(
            _mapper.ConfigurationProvider,
            query,
            cancellationToken);
    }

    public async Task<IReadOnlyList<DefectTypeDto>> GetAllActiveAsync(CancellationToken cancellationToken = default) =>
        await _unitOfWork.Repository<DefectType>()
            .Query()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ProjectTo<DefectTypeDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

    public async Task<DefectTypeDto> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _unitOfWork.Repository<DefectType>()
            .Query()
            .Where(t => t.Id == id)
            .ProjectTo<DefectTypeDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("Вид дефекта", id);

    public async Task<DefectTypeDto> CreateAsync(
        CreateDefectTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim();
        await EnsureCodeIsUniqueAsync(code, null, cancellationToken);

        var defectType = new DefectType
        {
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            DefaultSeverity = request.DefaultSeverity,
            IsActive = true
        };

        await _unitOfWork.Repository<DefectType>().AddAsync(defectType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(defectType.Id, cancellationToken);
    }

    public async Task<DefectTypeDto> UpdateAsync(
        int id,
        UpdateDefectTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        var defectType = await _unitOfWork.Repository<DefectType>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Вид дефекта", id);

        var code = request.Code.Trim();
        await EnsureCodeIsUniqueAsync(code, id, cancellationToken);

        defectType.Code = code;
        defectType.Name = request.Name.Trim();
        defectType.Description = request.Description?.Trim();
        defectType.DefaultSeverity = request.DefaultSeverity;
        defectType.IsActive = request.IsActive;

        _unitOfWork.Repository<DefectType>().Update(defectType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var defectType = await _unitOfWork.Repository<DefectType>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Вид дефекта", id);

        if (await _unitOfWork.Repository<Defect>().ExistsAsync(d => d.DefectTypeId == id, cancellationToken))
        {
            throw new DomainException(
                $"Вид дефекта «{defectType.Name}» используется в зарегистрированных дефектах. "
                + "Снимите отметку «Активен» вместо удаления.");
        }

        _unitOfWork.Repository<DefectType>().Remove(defectType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCodeIsUniqueAsync(string code, int? excludingId, CancellationToken cancellationToken)
    {
        var taken = await _unitOfWork.Repository<DefectType>()
            .ExistsAsync(t => t.Code == code && (excludingId == null || t.Id != excludingId), cancellationToken);

        if (taken)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(CreateDefectTypeRequest.Code)] = [$"Вид дефекта с кодом «{code}» уже существует."]
            });
        }
    }
}
