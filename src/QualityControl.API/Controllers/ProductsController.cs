using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.Products;
using QualityControl.Application.Features.Products.Dtos;
using QualityControl.Domain.Entities;

namespace QualityControl.API.Controllers;

/// <summary>
/// Справочник изделий.
/// </summary>
[ApiController]
[Route("api/products")]
[Authorize]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>Список изделий с поиском, фильтрацией, сортировкой и пагинацией.</summary>
    /// <remarks>
    /// Поиск идёт по наименованию, коду и обозначению чертежа.
    /// Сортировка: code, name, category, drawingNumber, createdAt.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductListItemDto>>> GetPaged(
        [FromQuery] ProductQuery query,
        CancellationToken cancellationToken) =>
        Ok(await _productService.GetPagedAsync(query, cancellationToken));

    /// <summary>Карточка изделия.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _productService.GetByIdAsync(id, cancellationToken));

    /// <summary>Создание изделия.</summary>
    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.ProductionManager}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> Create(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await _productService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    /// <summary>Изменение изделия.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.ProductionManager}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> Update(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _productService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Удаление изделия.</summary>
    /// <response code="409">Изделие используется в партиях — доступен только перевод в архив.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _productService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Перевод изделия в архив.</summary>
    /// <remarks>Архивное изделие сохраняет историю, но недоступно для новых партий.</remarks>
    [HttpPost("{id:int}/archive")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.ProductionManager}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> Archive(int id, CancellationToken cancellationToken) =>
        Ok(await _productService.ArchiveAsync(id, cancellationToken));

    /// <summary>Возврат изделия из архива.</summary>
    [HttpPost("{id:int}/restore")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.ProductionManager}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> Restore(int id, CancellationToken cancellationToken) =>
        Ok(await _productService.RestoreAsync(id, cancellationToken));
}
