using System.Text;
using System.Text.RegularExpressions;
using SecureKnowledgeBase.Application.Interfaces;

namespace SecureKnowledgeBase.Infrastructure.Services;

public class TextChunker : ITextChunker
{
    private const int MinChunkSize = 250;
    private const int TargetChunkSize = 500;
    private const int MaxChunkSize = 650;
    private const int OverlapSize = 60;

    private static readonly Regex HeaderRegex = new(
        @"^(#{1,6}\s+|Раздел\s+\d+|Глава\s+\d+|Статья\s+\d+|Пункт\s+\d+|\d+\.\s+)(.+)$",
        RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public IReadOnlyList<(string Text, string SectionHeader, int ChunkNumber)> ChunkText(string text)
    {
        var result = new List<(string Text, string SectionHeader, int ChunkNumber)>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return result;
        }

        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var currentSection = "Основная часть";
        var currentChunkBuilder = new StringBuilder();
        int chunkNumber = 1;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line))
            {
                if (currentChunkBuilder.Length > 0)
                {
                    currentChunkBuilder.AppendLine();
                }
                continue;
            }

            var headerMatch = HeaderRegex.Match(line);
            if (headerMatch.Success)
            {
                if (currentChunkBuilder.Length > 0)
                {
                    var chunkContent = currentChunkBuilder.ToString().Trim();
                    if (!string.IsNullOrEmpty(chunkContent))
                    {
                        result.Add((chunkContent, currentSection, chunkNumber++));
                    }
                    currentChunkBuilder.Clear();
                }

                currentSection = line.TrimStart('#', ' ').Trim();
                continue;
            }

            if (line.Length > MaxChunkSize)
            {
                var sentences = SplitIntoSentencesOrWords(line);
                foreach (var piece in sentences)
                {
                    AppendPiece(piece, ref currentChunkBuilder, currentSection, ref chunkNumber, result);
                }
            }
            else
            {
                AppendPiece(line, ref currentChunkBuilder, currentSection, ref chunkNumber, result);
            }
        }

        if (currentChunkBuilder.Length > 0)
        {
            var lastChunkContent = currentChunkBuilder.ToString().Trim();
            if (!string.IsNullOrEmpty(lastChunkContent))
            {
                result.Add((lastChunkContent, currentSection, chunkNumber));
            }
        }

        return result;
    }

    private static void AppendPiece(
        string piece,
        ref StringBuilder builder,
        string section,
        ref int chunkNumber,
        List<(string Text, string SectionHeader, int ChunkNumber)> result)
    {
        if (builder.Length + piece.Length + 1 > MaxChunkSize && builder.Length >= MinChunkSize)
        {
            var chunkContent = builder.ToString().Trim();
            result.Add((chunkContent, section, chunkNumber++));

            var overlap = GetOverlapText(chunkContent, OverlapSize);
            builder.Clear();
            if (!string.IsNullOrEmpty(overlap))
            {
                builder.Append(overlap).Append(' ');
            }
        }

        if (builder.Length > 0 && !builder.ToString().EndsWith("\n") && !builder.ToString().EndsWith(" "))
        {
            builder.Append(' ');
        }
        builder.Append(piece);
    }

    private static List<string> SplitIntoSentencesOrWords(string text)
    {
        var pieces = new List<string>();
        var sentenceRegex = new Regex(@"(?<=[.!?])\s+");
        var parts = sentenceRegex.Split(text);

        foreach (var part in parts)
        {
            if (part.Length <= MaxChunkSize)
            {
                pieces.Add(part);
            }
            else
            {
                var words = part.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var temp = new StringBuilder();
                foreach (var w in words)
                {
                    if (temp.Length + w.Length + 1 > TargetChunkSize && temp.Length > 0)
                    {
                        pieces.Add(temp.ToString());
                        temp.Clear();
                    }
                    if (temp.Length > 0)
                    {
                        temp.Append(' ');
                    }
                    temp.Append(w);
                }
                if (temp.Length > 0)
                {
                    pieces.Add(temp.ToString());
                }
            }
        }

        return pieces;
    }

    private static string GetOverlapText(string text, int length)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= length)
        {
            return string.Empty;
        }

        var startIndex = text.Length - length;
        var spaceIndex = text.IndexOf(' ', startIndex);
        if (spaceIndex >= 0 && spaceIndex < text.Length - 1)
        {
            return text.Substring(spaceIndex + 1);
        }

        return text.Substring(startIndex);
    }
}
