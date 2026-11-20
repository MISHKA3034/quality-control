using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using QualityControl.Application.Common.Exceptions;
using QualityControl.Application.Common.Extensions;
using QualityControl.Application.Common.Interfaces;
using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.Products.Dtos;
using QualityControl.Domain.Entities;
using QualityControl.Domain.Exceptions;

namespace QualityControl.Application.Features.Products;

/// <inheritdoc cref="IProductService"/>
public class ProductService : IProductService
{
    /// <summary>Sort keys accepted from the query string, mapped to real columns.</summary>
    private static readonly Dictionary<string, Expression<Func<Product, object?>>> Sorts =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["code"] = p => p.Code,
            ["name"] = p => p.Name,
            ["category"] = p => p.Category.Name,
            ["drawingNumber"] = p => p.DrawingNumber,
            ["createdAt"] = p => p.CreatedAt
        };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUserService _currentUser;

    public ProductService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IDateTimeProvider clock,
        ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _clock = clock;
        _currentUser = currentUser;
    }

    public Task<PagedResult<ProductListItemDto>> GetPagedAsync(
        ProductQuery query,
        CancellationToken cancellationToken = default)
    {
        var search = query.Search?.Trim();

        var source = _unitOfWork.Repository<Product>()
            .Query()
            .WhereIf(query.CategoryId is not null, p => p.CategoryId == query.CategoryId)
            .WhereIf(query.IsArchived is not null, p => p.IsArchived == query.IsArchived)
            .WhereIf(
                !string.IsNullOrWhiteSpace(search),
                p => EF.Functions.Like(p.Name, $"%{search}%")
                     || EF.Functions.Like(p.Code, $"%{search}%")
                     || (p.DrawingNumber != null && EF.Functions.Like(p.DrawingNumber, $"%{search}%")))
            .ApplySort(query.SortBy, query.SortDescending, Sorts, p => p.Code);

        return source.ToPagedResultAsync<Product, ProductListItemDto>(
            _mapper.ConfigurationProvider,
            query,
            cancellationToken);
    }

    public async Task<ProductDto> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _unitOfWork.Repository<Product>()
            .Query()
            .Where(p => p.Id == id)
            .ProjectTo<ProductDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("Изделие", id);

    public async Task<ProductDto> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim();

        await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);
        await EnsureCodeIsUniqueAsync(code, excludingId: null, cancellationToken);

        var product = new Product
        {
            Code = code,
            Name = request.Name.Trim(),
            CategoryId = request.CategoryId,
            Description = request.Description?.Trim(),
            DrawingNumber = request.DrawingNumber?.Trim(),
            UnitOfMeasure = request.UnitOfMeasure.Trim()
        };

        await _unitOfWork.Repository<Product>().AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(product.Id, cancellationToken);
    }

    public async Task<ProductDto> UpdateAsync(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Изделие", id);

        var code = request.Code.Trim();

        await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);
        await EnsureCodeIsUniqueAsync(code, excludingId: id, cancellationToken);

        product.Code = code;
        product.Name = request.Name.Trim();
        product.CategoryId = request.CategoryId;
        product.Description = request.Description?.Trim();
        product.DrawingNumber = request.DrawingNumber?.Trim();
        product.UnitOfMeasure = request.UnitOfMeasure.Trim();

        _unitOfWork.Repository<Product>().Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Изделие", id);

        // A product with batches is referenced by inspection acts. Removing it —
        // even softly — would orphan those documents, so archiving is the only
        // legitimate way to withdraw it from use.
        var hasBatches = await _unitOfWork.Repository<ProductionBatch>()
            .ExistsAsync(b => b.ProductId == id, cancellationToken);

        if (hasBatches)
        {
            throw new DomainException(
                $"Изделие «{product.Name}» используется в производственных партиях и не может быть удалено. "
                + "Переведите его в архив.");
        }

        _unitOfWork.Repository<Product>().Remove(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProductDto> ArchiveAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Изделие", id);

        product.Archive(_clock.UtcNow, _currentUser.UserId);

        _unitOfWork.Repository<Product>().Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ProductDto> RestoreAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Изделие", id);

        product.Restore();

        _unitOfWork.Repository<Product>().Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    private async Task EnsureCategoryExistsAsync(int categoryId, CancellationToken cancellationToken)
    {
        var exists = await _unitOfWork.Repository<ProductCategory>()
            .ExistsAsync(c => c.Id == categoryId, cancellationToken);

        if (!exists)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(CreateProductRequest.CategoryId)] = ["Указанная категория не найдена."]
            });
        }
    }

    private async Task EnsureCodeIsUniqueAsync(string code, int? excludingId, CancellationToken cancellationToken)
    {
        var taken = await _unitOfWork.Repository<Product>()
            .ExistsAsync(
                p => p.Code == code && (excludingId == null || p.Id != excludingId),
                cancellationToken);

        if (taken)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(CreateProductRequest.Code)] = [$"Изделие с кодом «{code}» уже существует."]
            });
        }
    }
}
