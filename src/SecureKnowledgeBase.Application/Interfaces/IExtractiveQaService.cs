using SecureKnowledgeBase.Application.DTOs;

namespace SecureKnowledgeBase.Application.Interfaces;

public interface IExtractiveQaService
{
    RagQueryResponseDto SynthesizeAnswer(string question, IReadOnlyList<RagSourceItemDto> sources, long elapsedMilliseconds);
}
