namespace SecureKnowledgeBase.Domain.Interfaces;

public interface IFileStorageService
{
    Task<(string StoredFileName, string Sha256Hash, long SizeBytes)> SaveFileAsync(Stream contentStream, string originalFileName, CancellationToken cancellationToken = default);
    Task<Stream?> GetFileStreamAsync(string storedFileName, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string storedFileName, CancellationToken cancellationToken = default);
    Task<bool> FileExistsAsync(string storedFileName, CancellationToken cancellationToken = default);
}
