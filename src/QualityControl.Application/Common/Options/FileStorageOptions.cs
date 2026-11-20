namespace QualityControl.Application.Common.Options;

/// <summary>
/// Upload policy and storage location, bound from the "FileStorage" section.
/// </summary>
public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>
    /// Root directory for uploads. Relative paths resolve against the content root.
    /// Kept outside wwwroot so nothing here is served as a static file: an uploaded
    /// image must only ever come back through the API, which checks authorisation.
    /// </summary>
    public string RootPath { get; set; } = "App_Data/uploads";

    public string DefectPhotosFolder { get; set; } = "defects";

    public long MaxPhotoSizeBytes { get; set; } = 10 * 1024 * 1024;

    public int MaxPhotosPerDefect { get; set; } = 20;

    /// <summary>
    /// Media types accepted for defect photographs. An allowlist, not a blocklist:
    /// anything not named here is refused.
    /// </summary>
    /// <remarks>
    /// Empty by default, and populated only from configuration. This looks like a
    /// missing default but is deliberate: the configuration binder *appends* to a
    /// non-empty collection rather than replacing it, so a code-side default would
    /// merge with appsettings — duplicating entries and, worse, making it impossible
    /// to remove a format by editing configuration. Startup validation rejects an
    /// empty list, so a missing section fails loudly instead of refusing every upload.
    /// </remarks>
    public string[] AllowedPhotoContentTypes { get; set; } = [];

    /// <summary>
    /// Extensions allowed on disk, checked together with the media type so a
    /// mislabelled upload cannot land as an executable name. Populated from
    /// configuration only — see <see cref="AllowedPhotoContentTypes"/>.
    /// </summary>
    public string[] AllowedPhotoExtensions { get; set; } = [];
}
