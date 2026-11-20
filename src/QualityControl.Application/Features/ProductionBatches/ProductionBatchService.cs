using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using QualityControl.Application.Common.Exceptions;
using QualityControl.Application.Common.Extensions;
using QualityControl.Application.Common.Interfaces;
using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.ProductionBatches.Dtos;
using QualityControl.Domain.Entities;
using QualityControl.Domain.Enums;
using QualityControl.Domain.Exceptions;

namespace QualityControl.Application.Features.ProductionBatches;

/// <inheritdoc cref="IProductionBatchService"/>
public class ProductionBatchService : IProductionBatchService
{
    private static readonly Dictionary<string, Expression<Func<ProductionBatch, object?>>> Sorts =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["batchNumber"] = b => b.BatchNumber,
            ["manufactureDate"] = b => b.ManufactureDate,
            ["product"] = b => b.Product.Name,
            ["department"] = b => b.Department.Name,
            ["quantity"] = b => b.Quantity,
            ["status"] = b => b.Status
        };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IAuditService _audit;

    public ProductionBatchService(IUnitOfWork unitOfWork, IMapper mapper, IAuditService audit)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _audit = audit;
    }

    public Task<PagedResult<ProductionBatchListItemDto>> GetPagedAsync(
        ProductionBatchQuery query,
        CancellationToken cancellationToken = default)
    {
        var source = BuildQuery(query)
            .ApplySort(query.SortBy, query.SortDescending, Sorts, b => b.ManufactureDate, defaultDescending: true);

        return source.ToPagedResultAsync<ProductionBatch, ProductionBatchListItemDto>(
            _mapper.ConfigurationProvider,
            query,
            cancellationToken);
    }

    /// <summary>
    /// Shared by the list endpoint and the Excel/PDF export, so an exported report
    /// always matches the filter the user is looking at.
    /// </summary>
    internal IQueryable<ProductionBatch> BuildQuery(ProductionBatchQuery query)
    {
        var search = query.Search?.Trim();

        // DateTo is inclusive of the whole day: users mean "up to and including this
        // date", but a stored 14:30 timestamp would fall outside `<= date at 00:00`.
        var dateTo = query.DateTo?.Date.AddDays(1);

        return _unitOfWork.Repository<ProductionBatch>()
            .Query()
            .WhereIf(query.ProductId is not null, b => b.ProductId == query.ProductId)
            .WhereIf(query.DepartmentId is not null, b => b.DepartmentId == query.DepartmentId)
            .WhereIf(query.ForemanId is not null, b => b.ForemanId == query.ForemanId)
            .WhereIf(query.Status is not null, b => b.Status == query.Status)
            .WhereIf(query.DateFrom is not null, b => b.ManufactureDate >= query.DateFrom!.Value.Date)
            .WhereIf(dateTo is not null, b => b.ManufactureDate < dateTo)
            .WhereIf(
                !string.IsNullOrWhiteSpace(search),
                b => EF.Functions.Like(b.BatchNumber, $"%{search}%")
                     || EF.Functions.Like(b.Product.Name, $"%{search}%")
                     || EF.Functions.Like(b.Product.Code, $"%{search}%"));
    }

    public async Task<ProductionBatchDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var dto = await _unitOfWork.Repository<ProductionBatch>()
            .Query()
            .Where(b => b.Id == id)
            .ProjectTo<ProductionBatchDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Партия", id);

        dto.AllowedNextStatuses = ProductionBatch.GetAllowedTransitions(dto.Status);
        return dto;
    }

    public async Task<IReadOnlyList<ProductionBatchListItemDto>> GetInspectableAsync(
        CancellationToken cancellationToken = default) =>
        await _unitOfWork.Repository<ProductionBatch>()
            .Query()
            .Where(b => b.Status != BatchStatus.Closed)
            .OrderByDescending(b => b.ManufactureDate)
            .ProjectTo<ProductionBatchListItemDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

    public async Task<ProductionBatchDto> CreateAsync(
        CreateProductionBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var batchNumber = request.BatchNumber.Trim();

        await EnsureBatchNumberIsUniqueAsync(batchNumber, null, cancellationToken);
        await EnsureProductIsUsableAsync(request.ProductId, cancellationToken);
        await EnsureDepartmentExistsAsync(request.DepartmentId, cancellationToken);
        await EnsureForemanExistsAsync(request.ForemanId, cancellationToken);

        var batch = new ProductionBatch
        {
            BatchNumber = batchNumber,
            ProductId = request.ProductId,
            ManufactureDate = request.ManufactureDate.Date,
            DepartmentId = request.DepartmentId,
            ForemanId = request.ForemanId,
            Quantity = request.Quantity,
            Notes = request.Notes?.Trim(),
            Status = BatchStatus.InProduction
        };

        await _unitOfWork.Repository<ProductionBatch>().AddAsync(batch, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(batch.Id, cancellationToken);
    }

    public async Task<ProductionBatchDto> UpdateAsync(
        int id,
        UpdateProductionBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var batch = await _unitOfWork.Repository<ProductionBatch>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Партия", id);

        // Once OTK has looked at the batch, its identity is fixed: an act already
        // refers to this product and quantity. Editing them would silently rewrite
        // what was inspected.
        if (batch.Status is not (BatchStatus.InProduction or BatchStatus.AwaitingInspection))
        {
            throw new DomainException(
                $"Партия {batch.BatchNumber} находится в статусе «{batch.Status}» и не может быть изменена.");
        }

        var batchNumber = request.BatchNumber.Trim();

        await EnsureBatchNumberIsUniqueAsync(batchNumber, id, cancellationToken);
        await EnsureProductIsUsableAsync(request.ProductId, cancellationToken);
        await EnsureDepartmentExistsAsync(request.DepartmentId, cancellationToken);
        await EnsureForemanExistsAsync(request.ForemanId, cancellationToken);

        batch.BatchNumber = batchNumber;
        batch.ProductId = request.ProductId;
        batch.ManufactureDate = request.ManufactureDate.Date;
        batch.DepartmentId = request.DepartmentId;
        batch.ForemanId = request.ForemanId;
        batch.Quantity = request.Quantity;
        batch.Notes = request.Notes?.Trim();

        _unitOfWork.Repository<ProductionBatch>().Update(batch);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ProductionBatchDto> ChangeStatusAsync(
        int id,
        ChangeBatchStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var batch = await _unitOfWork.Repository<ProductionBatch>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Партия", id);

        var previous = batch.Status;

        // The entity decides whether this move is legal.
        batch.TransitionTo(request.Status);

        _unitOfWork.Repository<ProductionBatch>().Update(batch);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(
            AuditAction.Update,
            nameof(ProductionBatch),
            id.ToString(),
            $"Статус партии {batch.BatchNumber}: {previous} → {batch.Status}. {request.Comment}".Trim(),
            cancellationToken: cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var batch = await _unitOfWork.Repository<ProductionBatch>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Партия", id);

        if (await _unitOfWork.Repository<QualityInspection>()
                .ExistsAsync(i => i.ProductionBatchId == id, cancellationToken))
        {
            throw new DomainException(
                $"По партии {batch.BatchNumber} оформлены акты проверки. Удаление невозможно.");
        }

        _unitOfWork.Repository<ProductionBatch>().Remove(batch);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureBatchNumberIsUniqueAsync(
        string batchNumber,
        int? excludingId,
        CancellationToken cancellationToken)
    {
        var taken = await _unitOfWork.Repository<ProductionBatch>()
            .ExistsAsync(
                b => b.BatchNumber == batchNumber && (excludingId == null || b.Id != excludingId),
                cancellationToken);

        if (taken)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(CreateProductionBatchRequest.BatchNumber)] =
                    [$"Партия с номером «{batchNumber}» уже зарегистрирована."]
            });
        }
    }

    private async Task EnsureProductIsUsableAsync(int productId, CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.Repository<Product>()
            .Query()
            .Where(p => p.Id == productId)
            .Select(p => new { p.Name, p.IsArchived })
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(CreateProductionBatchRequest.ProductId)] = ["Указанное изделие не найдено."]
            });
        }

        // This is the point of archiving: the record stays, but no new work starts.
        if (product.IsArchived)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(CreateProductionBatchRequest.ProductId)] =
                    [$"Изделие «{product.Name}» находится в архиве и недоступно для новых партий."]
            });
        }
    }

    private async Task EnsureDepartmentExistsAsync(int departmentId, CancellationToken cancellationToken)
    {
        if (!await _unitOfWork.Repository<Department>().ExistsAsync(d => d.Id == departmentId, cancellationToken))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(CreateProductionBatchRequest.DepartmentId)] = ["Указанный участок не найден."]
            });
        }
    }

    private async Task EnsureForemanExistsAsync(int foremanId, CancellationToken cancellationToken)
    {
        if (!await _unitOfWork.Repository<Employee>()
                .ExistsAsync(e => e.Id == foremanId && e.IsActive, cancellationToken))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(CreateProductionBatchRequest.ForemanId)] = ["Указанный мастер не найден или уволен."]
            });
        }
    }
}
