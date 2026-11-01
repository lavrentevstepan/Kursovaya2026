namespace SecureKnowledgeBase.Domain.Interfaces;

public interface ITextExtractor
{
    bool CanHandle(string extension);
    Task<string> ExtractTextAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
}
