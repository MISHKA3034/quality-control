using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.ProductCategories;
using QualityControl.Application.Features.ProductCategories.Dtos;
using QualityControl.Domain.Entities;

namespace QualityControl.API.Controllers;

/// <summary>
/// Справочник категорий изделий.
/// </summary>
[ApiController]
[Route("api/product-categories")]
[Authorize]
[Produces("application/json")]
public class ProductCategoriesController : ControllerBase
{
    private readonly IProductCategoryService _categoryService;

    public ProductCategoriesController(IProductCategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>Список категорий с пагинацией.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductCategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductCategoryDto>>> GetPaged(
        [FromQuery] ProductCategoryQuery query,
        CancellationToken cancellationToken) =>
        Ok(await _categoryService.GetPagedAsync(query, cancellationToken));

    /// <summary>Плоский список категорий для выпадающих списков.</summary>
    [HttpGet("lookup")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductCategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductCategoryDto>>> GetAll(
        CancellationToken cancellationToken) =>
        Ok(await _categoryService.GetAllAsync(cancellationToken));

    /// <summary>Карточка категории.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductCategoryDto>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _categoryService.GetByIdAsync(id, cancellationToken));

    /// <summary>Создание категории.</summary>
    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.ProductionManager}")]
    [ProducesResponseType(typeof(ProductCategoryDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductCategoryDto>> Create(
        CreateProductCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await _categoryService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }

    /// <summary>Изменение категории.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.ProductionManager}")]
    [ProducesResponseType(typeof(ProductCategoryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductCategoryDto>> Update(
        int id,
        UpdateProductCategoryRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _categoryService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Удаление категории.</summary>
    /// <response code="409">Категория содержит изделия или вложенные категории.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _categoryService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
