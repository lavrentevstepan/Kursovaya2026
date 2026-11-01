using SecureKnowledgeBase.Domain.Interfaces;

namespace SecureKnowledgeBase.Infrastructure.Services.Extractors;

public class CompositeTextExtractor : ITextExtractor
{
    private readonly IEnumerable<ITextExtractor> _extractors;

    public CompositeTextExtractor(IEnumerable<ITextExtractor> extractors)
    {
        _extractors = extractors;
    }

    public bool CanHandle(string extension)
    {
        return _extractors.Any(e => e.CanHandle(extension));
    }

    public async Task<string> ExtractTextAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName);
        var extractor = _extractors.FirstOrDefault(e => e.CanHandle(extension));
        if (extractor == null)
        {
            throw new NotSupportedException($"Формат файла '{extension}' не поддерживается для извлечения текста");
        }

        return await extractor.ExtractTextAsync(stream, extension, cancellationToken);
    }
}
