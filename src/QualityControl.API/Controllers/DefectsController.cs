using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.Defects;
using QualityControl.Application.Features.Defects.Dtos;
using QualityControl.Domain.Entities;

namespace QualityControl.API.Controllers;

/// <summary>
/// Дефекты и фотографии дефектов.
/// </summary>
[ApiController]
[Route("api/defects")]
[Authorize]
[Produces("application/json")]
public class DefectsController : ControllerBase
{
    private readonly IDefectService _defectService;

    public DefectsController(IDefectService defectService)
    {
        _defectService = defectService;
    }

    /// <summary>Список дефектов с поиском, фильтрацией, сортировкой и пагинацией.</summary>
    /// <remarks>
    /// Фильтры: акт, партия, изделие, вид дефекта, подразделение, контролёр,
    /// критичность, признак «открыт», диапазон дат.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DefectListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DefectListItemDto>>> GetPaged(
        [FromQuery] DefectQuery query,
        CancellationToken cancellationToken) =>
        Ok(await _defectService.GetPagedAsync(query, cancellationToken));

    /// <summary>Карточка дефекта с фотографиями.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(DefectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DefectDto>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _defectService.GetByIdAsync(id, cancellationToken));

    /// <summary>Регистрация дефекта по акту проверки.</summary>
    /// <response code="409">Акт оформлен с результатом «Соответствует требованиям».</response>
    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.QualityInspector}")]
    [ProducesResponseType(typeof(DefectDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DefectDto>> Create(
        CreateDefectRequest request,
        CancellationToken cancellationToken)
    {
        var defect = await _defectService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = defect.Id }, defect);
    }

    /// <summary>Изменение дефекта.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.QualityInspector}")]
    [ProducesResponseType(typeof(DefectDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DefectDto>> Update(
        int id,
        UpdateDefectRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _defectService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Удаление дефекта.</summary>
    /// <response code="409">По дефекту созданы корректирующие действия.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.QualityInspector}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _defectService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Загрузка фотографии дефекта.</summary>
    /// <param name="id">Идентификатор дефекта.</param>
    /// <param name="file">Файл изображения (JPEG, PNG или WebP).</param>
    /// <param name="caption">Необязательная подпись.</param>
    /// <response code="400">Файл пуст, слишком велик либо недопустимого типа.</response>
    [HttpPost("{id:int}/photos")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.QualityInspector}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(DefectPhotoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DefectPhotoDto>> UploadPhoto(
        int id,
        IFormFile file,
        [FromForm] string? caption,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["file"] = ["Файл не передан."]
            }));
        }

        await using var stream = file.OpenReadStream();

        var upload = new PhotoUpload(
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            caption);

        var photo = await _defectService.AddPhotoAsync(id, upload, cancellationToken);

        return CreatedAtAction(nameof(GetPhotoContent), new { id, photoId = photo.Id }, photo);
    }

    /// <summary>Содержимое файла фотографии.</summary>
    /// <remarks>
    /// Файлы отдаются через API, а не как статика: загруженные фотографии дефектов
    /// доступны только аутентифицированным пользователям.
    /// </remarks>
    [HttpGet("{id:int}/photos/{photoId:int}/content")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPhotoContent(
        int id,
        int photoId,
        CancellationToken cancellationToken)
    {
        var photo = await _defectService.GetPhotoContentAsync(id, photoId, cancellationToken);

        return File(photo.Content, photo.ContentType, photo.FileName);
    }

    /// <summary>Удаление фотографии дефекта.</summary>
    [HttpDelete("{id:int}/photos/{photoId:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.QualityInspector}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePhoto(int id, int photoId, CancellationToken cancellationToken)
    {
        await _defectService.DeletePhotoAsync(id, photoId, cancellationToken);
        return NoContent();
    }
}
