namespace QualityControl.Application.Common.Interfaces;

/// <summary>
/// Stores uploaded defect photographs. Abstracted so the local-disk implementation
/// can be swapped for object storage without touching the defect use cases.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves a file under <paramref name="folder"/> using a generated name.
    /// </summary>
    /// <param name="content">Stream to persist. The caller owns its disposal.</param>
    /// <param name="originalFileName">Client-supplied name; used only to derive the extension.</param>
    /// <param name="folder">Sub-folder of the storage root, e.g. "defects".</param>
    /// <returns>The generated name to store on the entity.</returns>
    Task<string> SaveAsync(
        Stream content,
        string originalFileName,
        string folder,
        CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string storedFileName, string folder, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storedFileName, string folder, CancellationToken cancellationToken = default);

    bool Exists(string storedFileName, string folder);
}
