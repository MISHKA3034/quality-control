using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QualityControl.Application.Common.Exceptions;
using QualityControl.Application.Common.Extensions;
using QualityControl.Application.Common.Interfaces;
using QualityControl.Application.Common.Models;
using QualityControl.Application.Common.Options;
using QualityControl.Application.Features.Defects.Dtos;
using QualityControl.Domain.Entities;
using QualityControl.Domain.Enums;
using QualityControl.Domain.Exceptions;

namespace QualityControl.Application.Features.Defects;

/// <inheritdoc cref="IDefectService"/>
public class DefectService : IDefectService
{
    private static readonly Dictionary<string, Expression<Func<Defect, object?>>> Sorts =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["detectedAt"] = d => d.DetectedAt,
            ["severity"] = d => d.Severity,
            ["defectType"] = d => d.DefectType.Name,
            ["batchNumber"] = d => d.QualityInspection.ProductionBatch.BatchNumber,
            ["product"] = d => d.QualityInspection.ProductionBatch.Product.Name
        };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IFileStorageService _files;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;
    private readonly FileStorageOptions _storage;
    private readonly ILogger<DefectService> _logger;

    public DefectService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IFileStorageService files,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAuditService audit,
        IOptions<FileStorageOptions> storage,
        ILogger<DefectService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _files = files;
        _currentUser = currentUser;
        _clock = clock;
        _audit = audit;
        _storage = storage.Value;
        _logger = logger;
    }

    public Task<PagedResult<DefectListItemDto>> GetPagedAsync(
        DefectQuery query,
        CancellationToken cancellationToken = default)
    {
        var source = BuildQuery(query)
            .ApplySort(query.SortBy, query.SortDescending, Sorts, d => d.DetectedAt, defaultDescending: true);

        return source.ToPagedResultAsync<Defect, DefectListItemDto>(
            _mapper.ConfigurationProvider,
            query,
            cancellationToken);
    }

    /// <summary>Shared by the list endpoint and the report exports.</summary>
    internal IQueryable<Defect> BuildQuery(DefectQuery query)
    {
        var search = query.Search?.Trim();
        var dateTo = query.DateTo?.Date.AddDays(1);

        return _unitOfWork.Repository<Defect>()
            .Query()
            .WhereIf(query.QualityInspectionId is not null, d => d.QualityInspectionId == query.QualityInspectionId)
            .WhereIf(
                query.ProductionBatchId is not null,
                d => d.QualityInspection.ProductionBatchId == query.ProductionBatchId)
            .WhereIf(
                query.ProductId is not null,
                d => d.QualityInspection.ProductionBatch.ProductId == query.ProductId)
            .WhereIf(query.DefectTypeId is not null, d => d.DefectTypeId == query.DefectTypeId)
            .WhereIf(
                query.DepartmentId is not null,
                d => d.QualityInspection.ProductionBatch.DepartmentId == query.DepartmentId)
            .WhereIf(query.InspectorId is not null, d => d.QualityInspection.InspectorId == query.InspectorId)
            .WhereIf(query.Severity is not null, d => d.Severity == query.Severity)
            .WhereIf(query.DateFrom is not null, d => d.DetectedAt >= query.DateFrom!.Value.Date)
            .WhereIf(dateTo is not null, d => d.DetectedAt < dateTo)

            // "Open" means the work is unfinished: no corrective action yet, or one
            // that has not reached Closed.
            .WhereIf(
                query.IsOpen == true,
                d => !d.CorrectiveActions.Any()
                     || d.CorrectiveActions.Any(a => a.Status != CorrectiveActionStatus.Closed))
            .WhereIf(
                query.IsOpen == false,
                d => d.CorrectiveActions.Any()
                     && d.CorrectiveActions.All(a => a.Status == CorrectiveActionStatus.Closed))
            .WhereIf(
                !string.IsNullOrWhiteSpace(search),
                d => EF.Functions.Like(d.Description, $"%{search}%")
                     || (d.Location != null && EF.Functions.Like(d.Location, $"%{search}%"))
                     || EF.Functions.Like(d.QualityInspection.ProductionBatch.BatchNumber, $"%{search}%")
                     || EF.Functions.Like(d.QualityInspection.ProductionBatch.Product.Name, $"%{search}%"));
    }

    public async Task<IReadOnlyList<DefectListItemDto>> GetForExportAsync(
        DefectQuery query,
        int maxRows,
        CancellationToken cancellationToken = default) =>
        await BuildQuery(query)
            .ApplySort(query.SortBy, query.SortDescending, Sorts, d => d.DetectedAt, defaultDescending: true)
            .Take(maxRows)
            .ProjectTo<DefectListItemDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

    public async Task<DefectDto> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _unitOfWork.Repository<Defect>()
            .Query()
            .Where(d => d.Id == id)
            .ProjectTo<DefectDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("Дефект", id);

    public async Task<DefectDto> CreateAsync(
        CreateDefectRequest request,
        CancellationToken cancellationToken = default)
    {
        var inspection = await _unitOfWork.Repository<QualityInspection>()
            .Query()
            .Where(i => i.Id == request.QualityInspectionId)
            .Select(i => new { i.Id, i.InspectionDate, i.Outcome, i.InspectionNumber })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(CreateDefectRequest.QualityInspectionId)] = ["Акт проверки не найден."]
            });

        // A conforming act says "no findings". Attaching a defect to it would leave
        // the record self-contradictory.
        if (inspection.Outcome is InspectionOutcome.Conforming)
        {
            throw new DomainException(
                $"Акт {inspection.InspectionNumber} оформлен с результатом «Соответствует требованиям». "
                + "Регистрация дефекта невозможна.");
        }

        if (!await _unitOfWork.Repository<DefectType>()
                .ExistsAsync(t => t.Id == request.DefectTypeId, cancellationToken))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(CreateDefectRequest.DefectTypeId)] = ["Указанный вид дефекта не найден."]
            });
        }

        var defect = new Defect
        {
            QualityInspectionId = request.QualityInspectionId,
            DefectTypeId = request.DefectTypeId,
            Description = request.Description.Trim(),
            Severity = request.Severity,
            Location = request.Location?.Trim(),
            Comment = request.Comment?.Trim(),
            AffectedQuantity = request.AffectedQuantity,
            DetectedAt = inspection.InspectionDate
        };

        await _unitOfWork.Repository<Defect>().AddAsync(defect, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(
            AuditAction.DefectRegistered,
            nameof(Defect),
            defect.Id.ToString(),
            $"Зарегистрирован дефект по акту {inspection.InspectionNumber}: {defect.Description}",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(defect.Id, cancellationToken);
    }

    public async Task<DefectDto> UpdateAsync(
        int id,
        UpdateDefectRequest request,
        CancellationToken cancellationToken = default)
    {
        var defect = await _unitOfWork.Repository<Defect>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Дефект", id);

        if (!await _unitOfWork.Repository<DefectType>()
                .ExistsAsync(t => t.Id == request.DefectTypeId, cancellationToken))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(UpdateDefectRequest.DefectTypeId)] = ["Указанный вид дефекта не найден."]
            });
        }

        defect.DefectTypeId = request.DefectTypeId;
        defect.Description = request.Description.Trim();
        defect.Severity = request.Severity;
        defect.Location = request.Location?.Trim();
        defect.Comment = request.Comment?.Trim();
        defect.AffectedQuantity = request.AffectedQuantity;

        _unitOfWork.Repository<Defect>().Update(defect);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var defect = await _unitOfWork.Repository<Defect>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Дефект", id);

        if (await _unitOfWork.Repository<CorrectiveAction>()
                .ExistsAsync(a => a.DefectId == id, cancellationToken))
        {
            throw new DomainException(
                "По дефекту созданы корректирующие действия. Удаление невозможно.");
        }

        _unitOfWork.Repository<Defect>().Remove(defect);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(
            AuditAction.Delete,
            nameof(Defect),
            id.ToString(),
            $"Удалён дефект: {defect.Description}",
            cancellationToken: cancellationToken);
    }

    public async Task<DefectPhotoDto> AddPhotoAsync(
        int defectId,
        PhotoUpload upload,
        CancellationToken cancellationToken = default)
    {
        if (!await _unitOfWork.Repository<Defect>().ExistsAsync(d => d.Id == defectId, cancellationToken))
        {
            throw new NotFoundException("Дефект", defectId);
        }

        ValidateUpload(upload);

        var photoCount = await _unitOfWork.Repository<DefectPhoto>()
            .CountAsync(p => p.DefectId == defectId, cancellationToken);

        if (photoCount >= _storage.MaxPhotosPerDefect)
        {
            throw new DomainException(
                $"К дефекту уже приложено максимальное количество фотографий ({_storage.MaxPhotosPerDefect}).");
        }

        var storedFileName = await _files.SaveAsync(
            upload.Content,
            upload.FileName,
            _storage.DefectPhotosFolder,
            cancellationToken);

        var photo = new DefectPhoto
        {
            DefectId = defectId,
            OriginalFileName = Path.GetFileName(upload.FileName),
            StoredFileName = storedFileName,
            ContentType = upload.ContentType,
            FileSizeBytes = upload.Length,
            Caption = upload.Caption?.Trim(),
            UploadedAt = _clock.UtcNow,
            UploadedByUserId = _currentUser.UserId
        };

        try
        {
            await _unitOfWork.Repository<DefectPhoto>().AddAsync(photo, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // The bytes are already on disk. Without this the file would linger with
            // no row pointing at it — an orphan nothing will ever clean up.
            await TryDeleteFileAsync(storedFileName, cancellationToken);
            throw;
        }

        return _mapper.Map<DefectPhotoDto>(photo);
    }

    public async Task<PhotoDownload> GetPhotoContentAsync(
        int defectId,
        int photoId,
        CancellationToken cancellationToken = default)
    {
        var photo = await _unitOfWork.Repository<DefectPhoto>()
            .FirstOrDefaultAsync(p => p.Id == photoId && p.DefectId == defectId, false, cancellationToken)
            ?? throw new NotFoundException("Фотография", photoId);

        var stream = await _files.OpenReadAsync(photo.StoredFileName, _storage.DefectPhotosFolder, cancellationToken);

        if (stream is null)
        {
            _logger.LogError(
                "Файл {StoredFileName} фотографии {PhotoId} отсутствует в хранилище.",
                photo.StoredFileName,
                photo.Id);

            throw new NotFoundException("Файл фотографии не найден в хранилище.");
        }

        return new PhotoDownload(stream, photo.ContentType, photo.OriginalFileName);
    }

    public async Task DeletePhotoAsync(int defectId, int photoId, CancellationToken cancellationToken = default)
    {
        var photo = await _unitOfWork.Repository<DefectPhoto>()
            .FirstOrDefaultAsync(p => p.Id == photoId && p.DefectId == defectId, true, cancellationToken)
            ?? throw new NotFoundException("Фотография", photoId);

        var storedFileName = photo.StoredFileName;

        _unitOfWork.Repository<DefectPhoto>().Remove(photo);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Row first, file second: if this fails we leak a file, which is recoverable.
        // The other order could leave a row pointing at nothing, which is not.
        await TryDeleteFileAsync(storedFileName, cancellationToken);
    }

    private void ValidateUpload(PhotoUpload upload)
    {
        var errors = new Dictionary<string, string[]>();

        if (upload.Length <= 0)
        {
            errors["file"] = ["Файл пуст."];
        }

        if (upload.Length > _storage.MaxPhotoSizeBytes)
        {
            var limitMb = _storage.MaxPhotoSizeBytes / 1024d / 1024d;
            errors["file"] = [$"Размер файла превышает допустимые {limitMb:0.#} МБ."];
        }

        if (!_storage.AllowedPhotoContentTypes.Contains(upload.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            errors["contentType"] =
                [$"Недопустимый тип файла. Разрешены: {string.Join(", ", _storage.AllowedPhotoContentTypes)}."];
        }

        var extension = Path.GetExtension(upload.FileName);

        // Both the declared type and the extension must be allowed. The media type
        // is client-supplied and trivially forged, so it is not trusted on its own.
        if (string.IsNullOrEmpty(extension)
            || !_storage.AllowedPhotoExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            errors["fileName"] =
                [$"Недопустимое расширение файла. Разрешены: {string.Join(", ", _storage.AllowedPhotoExtensions)}."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private async Task TryDeleteFileAsync(string storedFileName, CancellationToken cancellationToken)
    {
        try
        {
            await _files.DeleteAsync(storedFileName, _storage.DefectPhotosFolder, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Не удалось удалить файл {StoredFileName} из хранилища. Требуется ручная очистка.",
                storedFileName);
        }
    }
}
