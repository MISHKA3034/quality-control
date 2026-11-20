using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using QualityControl.Application.Common.Exceptions;
using QualityControl.Application.Common.Extensions;
using QualityControl.Application.Common.Interfaces;
using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.ProductCategories.Dtos;
using QualityControl.Domain.Entities;
using QualityControl.Domain.Exceptions;

namespace QualityControl.Application.Features.ProductCategories;

/// <inheritdoc cref="IProductCategoryService"/>
public class ProductCategoryService : IProductCategoryService
{
    private static readonly Dictionary<string, Expression<Func<ProductCategory, object?>>> Sorts =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["code"] = c => c.Code,
            ["name"] = c => c.Name
        };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProductCategoryService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public Task<PagedResult<ProductCategoryDto>> GetPagedAsync(
        ProductCategoryQuery query,
        CancellationToken cancellationToken = default)
    {
        var search = query.Search?.Trim();

        var source = _unitOfWork.Repository<ProductCategory>()
            .Query()
            .WhereIf(query.ParentCategoryId is not null, c => c.ParentCategoryId == query.ParentCategoryId)
            .WhereIf(
                !string.IsNullOrWhiteSpace(search),
                c => EF.Functions.Like(c.Name, $"%{search}%") || EF.Functions.Like(c.Code, $"%{search}%"))
            .ApplySort(query.SortBy, query.SortDescending, Sorts, c => c.Name);

        return source.ToPagedResultAsync<ProductCategory, ProductCategoryDto>(
            _mapper.ConfigurationProvider,
            query,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ProductCategoryDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _unitOfWork.Repository<ProductCategory>()
            .Query()
            .OrderBy(c => c.Name)
            .ProjectTo<ProductCategoryDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

    public async Task<ProductCategoryDto> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _unitOfWork.Repository<ProductCategory>()
            .Query()
            .Where(c => c.Id == id)
            .ProjectTo<ProductCategoryDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("Категория", id);

    public async Task<ProductCategoryDto> CreateAsync(
        CreateProductCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim();
        await EnsureCodeIsUniqueAsync(code, null, cancellationToken);
        await EnsureParentExistsAsync(request.ParentCategoryId, cancellationToken);

        var category = new ProductCategory
        {
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            ParentCategoryId = request.ParentCategoryId
        };

        await _unitOfWork.Repository<ProductCategory>().AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(category.Id, cancellationToken);
    }

    public async Task<ProductCategoryDto> UpdateAsync(
        int id,
        UpdateProductCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Repository<ProductCategory>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Категория", id);

        var code = request.Code.Trim();
        await EnsureCodeIsUniqueAsync(code, id, cancellationToken);
        await EnsureParentExistsAsync(request.ParentCategoryId, cancellationToken);
        await EnsureParentIsNotCyclicAsync(id, request.ParentCategoryId, cancellationToken);

        category.Code = code;
        category.Name = request.Name.Trim();
        category.Description = request.Description?.Trim();
        category.ParentCategoryId = request.ParentCategoryId;

        _unitOfWork.Repository<ProductCategory>().Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Repository<ProductCategory>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Категория", id);

        var hasProducts = await _unitOfWork.Repository<Product>()
            .ExistsAsync(p => p.CategoryId == id, cancellationToken);

        if (hasProducts)
        {
            throw new DomainException(
                $"Категория «{category.Name}» содержит изделия и не может быть удалена.");
        }

        var hasChildren = await _unitOfWork.Repository<ProductCategory>()
            .ExistsAsync(c => c.ParentCategoryId == id, cancellationToken);

        if (hasChildren)
        {
            throw new DomainException(
                $"Категория «{category.Name}» содержит вложенные категории и не может быть удалена.");
        }

        _unitOfWork.Repository<ProductCategory>().Remove(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCodeIsUniqueAsync(string code, int? excludingId, CancellationToken cancellationToken)
    {
        var taken = await _unitOfWork.Repository<ProductCategory>()
            .ExistsAsync(c => c.Code == code && (excludingId == null || c.Id != excludingId), cancellationToken);

        if (taken)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(CreateProductCategoryRequest.Code)] = [$"Категория с кодом «{code}» уже существует."]
            });
        }
    }

    private async Task EnsureParentExistsAsync(int? parentId, CancellationToken cancellationToken)
    {
        if (parentId is null)
        {
            return;
        }

        var exists = await _unitOfWork.Repository<ProductCategory>()
            .ExistsAsync(c => c.Id == parentId, cancellationToken);

        if (!exists)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(CreateProductCategoryRequest.ParentCategoryId)] = ["Родительская категория не найдена."]
            });
        }
    }

    /// <summary>
    /// Walks up from the proposed parent. If we meet <paramref name="id"/> on the way,
    /// the move would form a cycle and orphan the branch from the tree root.
    /// </summary>
    private async Task EnsureParentIsNotCyclicAsync(
        int id,
        int? parentId,
        CancellationToken cancellationToken)
    {
        if (parentId is null)
        {
            return;
        }

        if (parentId == id)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(UpdateProductCategoryRequest.ParentCategoryId)] =
                    ["Категория не может быть родителем самой себя."]
            });
        }

        var repository = _unitOfWork.Repository<ProductCategory>();
        var visited = new HashSet<int> { id };
        var currentId = parentId;

        while (currentId is not null)
        {
            if (!visited.Add(currentId.Value))
            {
                break;
            }

            var parent = await repository.Query()
                .Where(c => c.Id == currentId)
                .Select(c => new { c.ParentCategoryId })
                .FirstOrDefaultAsync(cancellationToken);

            if (parent is null)
            {
                break;
            }

            if (parent.ParentCategoryId == id)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    [nameof(UpdateProductCategoryRequest.ParentCategoryId)] =
                        ["Нельзя переместить категорию в собственную подкатегорию."]
                });
            }

            currentId = parent.ParentCategoryId;
        }
    }
}
