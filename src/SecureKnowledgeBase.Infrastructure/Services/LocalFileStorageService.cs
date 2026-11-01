using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using SecureKnowledgeBase.Domain.Interfaces;
using SecureKnowledgeBase.Infrastructure.Configuration;

namespace SecureKnowledgeBase.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storageDirectory;

    public LocalFileStorageService(IOptions<SecurityOptions> options)
    {
        var configuredPath = options.Value.StorageDirectory;
        if (Path.IsPathRooted(configuredPath))
        {
            _storageDirectory = configuredPath;
        }
        else
        {
            _storageDirectory = Path.Combine(Directory.GetCurrentDirectory(), configuredPath);
        }

        if (!Directory.Exists(_storageDirectory))
        {
            Directory.CreateDirectory(_storageDirectory);
        }
    }

    public async Task<(string StoredFileName, string Sha256Hash, long SizeBytes)> SaveFileAsync(Stream contentStream, string originalFileName, CancellationToken cancellationToken = default)
    {
        var safeOriginalName = Path.GetFileName(originalFileName);
        var extension = Path.GetExtension(safeOriginalName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var destinationPath = GetSafePath(storedFileName);

        using var sha256 = SHA256.Create();
        await using var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

        var buffer = new byte[81920];
        int bytesRead;
        long totalBytes = 0;

        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            await destinationStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
            totalBytes += bytesRead;
        }

        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        var hashBytes = sha256.Hash ?? Array.Empty<byte>();
        var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return (storedFileName, hashString, totalBytes);
    }

    public Task<Stream?> GetFileStreamAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        var safePath = GetSafePath(storedFileName);
        if (!File.Exists(safePath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream fileStream = new FileStream(safePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        return Task.FromResult<Stream?>(fileStream);
    }

    public Task<bool> DeleteFileAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        var safePath = GetSafePath(storedFileName);
        if (File.Exists(safePath))
        {
            File.Delete(safePath);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task<bool> FileExistsAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        var safePath = GetSafePath(storedFileName);
        return Task.FromResult(File.Exists(safePath));
    }

    private string GetSafePath(string fileName)
    {
        var sanitized = Path.GetFileName(fileName);
        var fullPath = Path.GetFullPath(Path.Combine(_storageDirectory, sanitized));
        var fullStorageDir = Path.GetFullPath(_storageDirectory);

        if (!fullPath.StartsWith(fullStorageDir, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Обнаружена попытка выхода за пределы директории хранилища");
        }

        return fullPath;
    }
}
