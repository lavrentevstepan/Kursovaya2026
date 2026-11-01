using System.IO.Compression;
using System.Text;
using SecureKnowledgeBase.Domain.Interfaces;
using SecureKnowledgeBase.Infrastructure.Services.Extractors;

namespace SecureKnowledgeBase.UnitTests.Services;

public class TextExtractorTests
{
    [Fact]
    public async Task PlainTextExtractor_TxtAndMd_ExtractsContentCorrectly()
    {
        var extractor = new PlainTextExtractor();
        Assert.True(extractor.CanHandle(".txt"));
        Assert.True(extractor.CanHandle(".md"));
        Assert.False(extractor.CanHandle(".pdf"));

        var sampleText = "Тестовый текст регламента безопасности на русском языке";
        var bytes = Encoding.UTF8.GetBytes(sampleText);
        using var stream = new MemoryStream(bytes);

        var result = await extractor.ExtractTextAsync(stream, "sample.txt");
        Assert.Equal(sampleText, result);
    }

    [Fact]
    public async Task DocxTextExtractor_ValidDocxZip_ExtractsParagraphs()
    {
        var extractor = new DocxTextExtractor();
        Assert.True(extractor.CanHandle(".docx"));
        Assert.False(extractor.CanHandle(".txt"));

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            var entry = archive.CreateEntry("word/document.xml");
            await using var entryStream = entry.Open();
            var xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body><w:p><w:r><w:t>Заголовок первого раздела</w:t></w:r></w:p><w:p><w:r><w:t>Текст регламента ИБ</w:t></w:r></w:p></w:body></w:document>";
            var xmlBytes = Encoding.UTF8.GetBytes(xml);
            await entryStream.WriteAsync(xmlBytes);
        }

        memoryStream.Position = 0;
        var extracted = await extractor.ExtractTextAsync(memoryStream, "document.docx");

        Assert.Contains("Заголовок первого раздела", extracted);
        Assert.Contains("Текст регламента ИБ", extracted);
    }

    [Fact]
    public async Task CompositeTextExtractor_UnsupportedExtension_ThrowsNotSupportedException()
    {
        var composite = new CompositeTextExtractor(new ITextExtractor[]
        {
            new PlainTextExtractor()
        });

        using var stream = new MemoryStream();
        await Assert.ThrowsAsync<NotSupportedException>(() => composite.ExtractTextAsync(stream, "file.unknown"));
    }
}

