using System.Text;
using SecureKnowledgeBase.Domain.Interfaces;

namespace SecureKnowledgeBase.Infrastructure.Services.Extractors;

public class PlainTextExtractor : ITextExtractor
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt",
        ".md"
    };

    public bool CanHandle(string extension)
    {
        return SupportedExtensions.Contains(extension);
    }

    public async Task<string> ExtractTextAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
