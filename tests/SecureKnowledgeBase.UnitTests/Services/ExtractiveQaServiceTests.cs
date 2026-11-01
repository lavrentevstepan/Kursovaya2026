using SecureKnowledgeBase.Application.DTOs;
using SecureKnowledgeBase.Domain.Enums;
using SecureKnowledgeBase.Infrastructure.Services;

namespace SecureKnowledgeBase.UnitTests.Services;

public class ExtractiveQaServiceTests
{
    private readonly ExtractiveQaService _service = new();

    [Fact]
    public void SynthesizeAnswer_MatchingQuestionAndSources_ReturnsSynthesizedAnswer()
    {
        var question = "Какая минимальная длина пароля?";
        var sources = new List<RagSourceItemDto>
        {
            new()
            {
                ChunkId = Guid.NewGuid(),
                DocumentId = Guid.NewGuid(),
                DocumentTitle = "Политика парольной безопасности",
                SectionHeader = "Требования к сложности",
                AccessLevel = AccessLevel.PublicInternal,
                RelevanceScore = 8.5,
                FullText = "Минимальная длина пароля для всех сотрудников составляет не менее 12 символов. Пароль должен содержать цифры и буквы."
            }
        };

        var result = _service.SynthesizeAnswer(question, sources, 5);

        Assert.True(result.HasFoundAnswer);
        Assert.Contains("12 символов", result.Answer);
        Assert.Contains("Политика парольной безопасности", result.Answer);
        Assert.Equal(RagMode.ExtractiveOnly, result.ModeUsed);
    }

    [Fact]
    public void SynthesizeAnswer_IrrelevantQuestion_ReturnsNotFound()
    {
        var question = "Сколько колец у планеты Сатурн?";
        var sources = new List<RagSourceItemDto>
        {
            new()
            {
                ChunkId = Guid.NewGuid(),
                DocumentId = Guid.NewGuid(),
                DocumentTitle = "Политика паролей",
                SectionHeader = "Общие положения",
                AccessLevel = AccessLevel.PublicInternal,
                RelevanceScore = 0.5,
                FullText = "Настоящий регламент определяет порядок создания учетных записей."
            }
        };

        var result = _service.SynthesizeAnswer(question, sources, 2);

        Assert.False(result.HasFoundAnswer);
        Assert.Contains("не найден", result.Answer);
    }

    [Fact]
    public void SynthesizeAnswer_EmptySources_ReturnsNotFound()
    {
        var result = _service.SynthesizeAnswer("Какой-то вопрос", new List<RagSourceItemDto>(), 1);

        Assert.False(result.HasFoundAnswer);
        Assert.Contains("не найден", result.Answer);
    }
}
