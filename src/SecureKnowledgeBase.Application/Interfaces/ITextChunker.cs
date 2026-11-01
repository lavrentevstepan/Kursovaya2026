namespace SecureKnowledgeBase.Application.Interfaces;

public interface ITextChunker
{
    IReadOnlyList<(string Text, string SectionHeader, int ChunkNumber)> ChunkText(string text);
}
