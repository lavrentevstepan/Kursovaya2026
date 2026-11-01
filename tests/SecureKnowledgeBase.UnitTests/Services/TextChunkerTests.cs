using SecureKnowledgeBase.Infrastructure.Services;

namespace SecureKnowledgeBase.UnitTests.Services;

public class TextChunkerTests
{
    [Fact]
    public void ChunkText_EmptyOrWhitespace_ReturnsEmptyList()
    {
        var chunker = new TextChunker();
        var chunks = chunker.ChunkText("");
        Assert.Empty(chunks);

        var nullChunks = chunker.ChunkText("   \r\n   ");
        Assert.Empty(nullChunks);
    }

    [Fact]
    public void ChunkText_WithHeaders_ExtractsSectionHeaders()
    {
        var chunker = new TextChunker();
        var text = @"# Раздел 1: Введение
Это вступительная часть регламента информационной безопасности предприятия.
Здесь описываются цели и задачи системы управления доступом.

## Раздел 2: Парольная защита
Все пароли пользователей должны иметь длину не менее 12 символов.
Обязательно наличие цифр, специальных символов и заглавных букв.";

        var chunks = chunker.ChunkText(text);

        Assert.NotEmpty(chunks);
        Assert.Contains(chunks, c => c.SectionHeader.Contains("Раздел 1") || c.SectionHeader.Contains("Введение"));
        Assert.Contains(chunks, c => c.SectionHeader.Contains("Раздел 2") || c.SectionHeader.Contains("Парольная защита"));
    }

    [Fact]
    public void ChunkText_LongText_ProducesMultipleSequentialChunks()
    {
        var chunker = new TextChunker();
        var sentence = "Каждый сотрудник обязан проходить периодический инструктаж по информационной безопасности. ";
        var longText = string.Concat(Enumerable.Repeat(sentence, 30));

        var chunks = chunker.ChunkText(longText);

        Assert.True(chunks.Count > 1);
        for (int i = 0; i < chunks.Count; i++)
        {
            Assert.Equal(i + 1, chunks[i].ChunkNumber);
            Assert.True(chunks[i].Text.Length > 0);
        }
    }
}
