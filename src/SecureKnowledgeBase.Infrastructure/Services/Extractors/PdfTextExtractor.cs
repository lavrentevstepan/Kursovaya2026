using System.Text;
using SecureKnowledgeBase.Domain.Interfaces;
using UglyToad.PdfPig;

namespace SecureKnowledgeBase.Infrastructure.Services.Extractors;

public class PdfTextExtractor : ITextExtractor
{
    public bool CanHandle(string extension)
    {
        return string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase);
    }

    public Task<string> ExtractTextAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        using var pdf = PdfDocument.Open(memoryStream);
        var sb = new StringBuilder();

        foreach (var page in pdf.GetPages())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var text = page.Text;
            if (!string.IsNullOrWhiteSpace(text))
            {
                sb.AppendLine(text.Trim());
            }
        }

        return Task.FromResult(sb.ToString().Trim());
    }
}
