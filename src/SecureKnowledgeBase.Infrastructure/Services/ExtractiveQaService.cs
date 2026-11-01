using System.Text;
using System.Text.RegularExpressions;
using SecureKnowledgeBase.Application.DTOs;
using SecureKnowledgeBase.Application.Interfaces;

namespace SecureKnowledgeBase.Infrastructure.Services;

public class ExtractiveQaService : IExtractiveQaService
{
    private static readonly HashSet<string> RussianStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "в", "на", "с", "со", "по", "к", "ко", "из", "за", "от", "для", "о", "об", "обо",
        "и", "а", "но", "да", "или", "ли", "что", "как", "где", "куда", "когда", "почему",
        "зачем", "кто", "чем", "какой", "какая", "какое", "какие", "каком", "какую", "это",
        "этот", "эта", "эти", "то", "тот", "быть", "был", "была", "были", "будет", "есть"
    };

    public RagQueryResponseDto SynthesizeAnswer(string question, IReadOnlyList<RagSourceItemDto> sources, long elapsedMilliseconds)
    {
        var response = new RagQueryResponseDto
        {
            Question = question ?? string.Empty,
            ModeUsed = RagMode.ExtractiveOnly,
            ModelName = "Extractive-QA (Deterministic)",
            ElapsedMilliseconds = elapsedMilliseconds,
            Sources = sources?.ToList() ?? new List<RagSourceItemDto>()
        };

        if (string.IsNullOrWhiteSpace(question) || sources == null || sources.Count == 0)
        {
            response.Answer = "Точный ответ в доступной базе знаний не найден.";
            response.HasFoundAnswer = false;
            return response;
        }

        var questionKeywords = ExtractKeywords(question);
        if (questionKeywords.Count == 0)
        {
            response.Answer = "Точный ответ в доступной базе знаний не найден.";
            response.HasFoundAnswer = false;
            return response;
        }

        var sentenceMatches = new List<(string Sentence, RagSourceItemDto Source, double Score)>();

        foreach (var source in sources)
        {
            var text = source.FullText;
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var sentences = SplitIntoSentences(text);
            foreach (var rawSentence in sentences)
            {
                var sentence = rawSentence.Trim();
                if (sentence.Length < 15)
                {
                    continue;
                }

                var sentenceScore = CalculateSentenceScore(sentence, questionKeywords);
                if (sentenceScore > 0)
                {
                    sentenceMatches.Add((sentence, source, sentenceScore + (source.RelevanceScore * 0.1)));
                }
            }
        }

        var topSentences = sentenceMatches
            .OrderByDescending(x => x.Score)
            .DistinctBy(x => x.Sentence)
            .Take(4)
            .ToList();

        if (topSentences.Count == 0 || topSentences[0].Score < 1.0)
        {
            response.Answer = "Точный ответ в доступной базе знаний не найден.";
            response.HasFoundAnswer = false;
            return response;
        }

        var sb = new StringBuilder();
        sb.AppendLine("На основе доступных регламентов и документов найдены следующие сведения:\n");

        foreach (var match in topSentences)
        {
            sb.AppendLine($"• «{match.Sentence}»");
            sb.AppendLine($"  [Источник: {match.Source.DocumentTitle} | Раздел: {match.Source.SectionHeader}]");
            sb.AppendLine();
        }

        response.Answer = sb.ToString().TrimEnd();
        response.HasFoundAnswer = true;
        return response;
    }

    private static HashSet<string> ExtractKeywords(string text)
    {
        var words = Regex.Split(text.ToLowerInvariant(), @"[^\p{L}\p{Nd}]+")
            .Where(w => w.Length > 2 && !RussianStopWords.Contains(w))
            .Select(Stem)
            .ToHashSet();

        return words;
    }

    private static double CalculateSentenceScore(string sentence, HashSet<string> questionKeywords)
    {
        var sentenceWords = Regex.Split(sentence.ToLowerInvariant(), @"[^\p{L}\p{Nd}]+")
            .Where(w => w.Length > 2)
            .Select(Stem)
            .ToList();

        double score = 0;
        foreach (var word in sentenceWords)
        {
            if (questionKeywords.Contains(word))
            {
                score += 1.5;
            }
        }

        return score;
    }

    private static List<string> SplitIntoSentences(string text)
    {
        var parts = Regex.Split(text, @"(?<=[.!?])\s+(?=[А-ЯA-Z0-9«""#])");
        return parts.Select(p => p.Trim()).Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
    }

    private static string Stem(string word)
    {
        if (word.Length <= 4)
        {
            return word;
        }

        string[] suffixes =
        {
            "овать", "евать", "ивать", "ывать", "ение", "ения", "ению", "ениях",
            "ость", "ости", "остью", "остях", "ного", "ному", "ными", "ным",
            "ный", "ная", "ное", "ные", "ных", "ном", "ной",
            "ами", "ями", "ей", "ем", "ом", "ам", "ям", "ах", "ях",
            "ов", "ев", "ую", "юю", "ые", "ие", "ый", "ий", "ой", "ая", "яя",
            "ет", "ут", "ют", "ит", "ат", "ят", "ил", "ила", "или",
            "а", "е", "и", "й", "о", "у", "ы", "ь", "я"
        };

        foreach (var s in suffixes)
        {
            if (word.EndsWith(s) && word.Length - s.Length >= 3)
            {
                return word.Substring(0, word.Length - s.Length);
            }
        }

        return word;
    }
}
