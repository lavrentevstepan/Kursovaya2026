using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using SecureKnowledgeBase.Domain.Interfaces;

namespace SecureKnowledgeBase.Infrastructure.Services.Extractors;

public class DocxTextExtractor : ITextExtractor
{
    public bool CanHandle(string extension)
    {
        return string.Equals(extension, ".docx", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> ExtractTextAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read, leaveOpen: false);
        var entry = archive.GetEntry("word/document.xml");
        if (entry == null)
        {
            return string.Empty;
        }

        await using var entryStream = entry.Open();
        var xDoc = await XDocument.LoadAsync(entryStream, LoadOptions.None, cancellationToken);
        if (xDoc.Root == null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        var paragraphs = xDoc.Descendants().Where(e => e.Name.LocalName == "p");

        foreach (var p in paragraphs)
        {
            var textElements = p.Descendants().Where(e => e.Name.LocalName == "t");
            var paragraphText = string.Concat(textElements.Select(t => t.Value));
            if (!string.IsNullOrWhiteSpace(paragraphText))
            {
                sb.AppendLine(paragraphText.Trim());
            }
        }

        return sb.ToString().Trim();
    }
}
