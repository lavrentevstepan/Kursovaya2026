using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SecureKnowledgeBase.Infrastructure.Configuration;
using SecureKnowledgeBase.Infrastructure.Services;

namespace SecureKnowledgeBase.UnitTests.Services;

public class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly LocalFileStorageService _service;

    public LocalFileStorageServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "skb_test_storage_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);

        var options = Options.Create(new SecurityOptions
        {
            StorageDirectory = _testDirectory
        });

        _service = new LocalFileStorageService(options);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
            }
        }
    }

    [Fact]
    public async Task SaveFileAsync_ValidStream_SavesFileAndComputesSha256()
    {
        var content = "Security Coursework Variant 25 Test Content";
        var bytes = Encoding.UTF8.GetBytes(content);
        using var stream = new MemoryStream(bytes);

        using var sha = SHA256.Create();
        var expectedHash = Convert.ToHexString(sha.ComputeHash(bytes)).ToLowerInvariant();

        var (storedFileName, sha256Hash, sizeBytes) = await _service.SaveFileAsync(stream, "test_document.txt");

        Assert.NotNull(storedFileName);
        Assert.EndsWith(".txt", storedFileName);
        Assert.Equal(expectedHash, sha256Hash);
        Assert.Equal(bytes.Length, sizeBytes);

        var exists = await _service.FileExistsAsync(storedFileName);
        Assert.True(exists);

        var readStream = await _service.GetFileStreamAsync(storedFileName);
        Assert.NotNull(readStream);
        using (var reader = new StreamReader(readStream))
        {
            var readContent = await reader.ReadToEndAsync();
            Assert.Equal(content, readContent);
        }

        var deleted = await _service.DeleteFileAsync(storedFileName);
        Assert.True(deleted);

        var existsAfterDelete = await _service.FileExistsAsync(storedFileName);
        Assert.False(existsAfterDelete);
    }

    [Fact]
    public async Task DeleteFileAsync_NonExistentFile_ReturnsFalse()
    {
        var result = await _service.DeleteFileAsync("non_existent_file.txt");
        Assert.False(result);
    }

    [Fact]
    public async Task GetFileStreamAsync_NonExistentFile_ReturnsNull()
    {
        var result = await _service.GetFileStreamAsync("non_existent_file.txt");
        Assert.Null(result);
    }
}
