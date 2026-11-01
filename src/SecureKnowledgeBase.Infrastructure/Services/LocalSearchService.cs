using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SecureKnowledgeBase.Application.DTOs;
using SecureKnowledgeBase.Application.Interfaces;
using SecureKnowledgeBase.Domain.Enums;
using SecureKnowledgeBase.Infrastructure.Persistence;

namespace SecureKnowledgeBase.Infrastructure.Services;

public class LocalSearchService : SecureKnowledgeBase.Application.Interfaces.ISearchService, SecureKnowledgeBase.Domain.Interfaces.ISearchService
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;

    private static readonly HashSet<string> RussianStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "и", "в", "на", "с", "по", "для", "о", "об", "к", "от", "из", "за", "над", "под", "при",
        "до", "то", "что", "как", "а", "но", "да", "или", "ли", "бы", "же", "не", "ни"
    };

    private static readonly Regex WordRegex = new(@"[\p{L}\p{N}]+", RegexOptions.Compiled);

    public LocalSearchService(AppDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<SearchResultDto> SearchAsync(SearchRequestDto request, AccessLevel maxAccessLevel, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return new SearchResultDto
            {
                Query = string.Empty,
                TotalHits = 0,
                Hits = Array.Empty<SearchHitDto>(),
                ElapsedMilliseconds = 0
            };
        }

        var normalizedQuery = request.Query.Trim().ToLowerInvariant();
        var queryTerms = WordRegex.Matches(normalizedQuery)
            .Select(m => m.Value)
            .Where(w => w.Length > 1 && !RussianStopWords.Contains(w))
            .ToList();

        if (queryTerms.Count == 0)
        {
            queryTerms = WordRegex.Matches(normalizedQuery)
                .Select(m => m.Value)
                .ToList();
        }

        var chunksQuery = _context.DocumentChunks
            .Include(c => c.Document)
            .Where(c => c.Document != null && c.Document.Status != DocumentStatus.Deleted)
            .Where(c => (int)c.Document!.AccessLevel <= (int)maxAccessLevel);

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var cat = request.Category.Trim();
            chunksQuery = chunksQuery.Where(c => c.Document!.Category.ToLower() == cat.ToLower());
        }

        var candidateChunks = await chunksQuery.ToListAsync(cancellationToken);
        var hits = new List<SearchHitDto>();

        foreach (var chunk in candidateChunks)
        {
            var doc = chunk.Document!;
            var score = CalculateScore(doc.Title, doc.Description, chunk.SectionHeader, chunk.TextContent, normalizedQuery, queryTerms);

            if (score > 0.05)
            {
                var snippet = CreateSnippet(chunk.TextContent, queryTerms);
                hits.Add(new SearchHitDto
                {
                    DocumentId = doc.Id,
                    DocumentTitle = doc.Title,
                    Category = doc.Category,
                    AccessLevel = doc.AccessLevel,
                    ChunkId = chunk.Id,
                    ChunkNumber = chunk.ChunkNumber,
                    Snippet = snippet,
                    FullText = chunk.TextContent,
                    SectionHeader = string.IsNullOrWhiteSpace(chunk.SectionHeader) ? "Основная часть" : chunk.SectionHeader,
                    Score = Math.Round(score, 2)
                });
            }
        }

        var rankedHits = hits
            .OrderByDescending(h => h.Score)
            .Take(request.MaxResults > 0 ? request.MaxResults : 10)
            .ToList();

        sw.Stop();

        await _auditService.LogAsync(
            "user",
            AuditAction.AssistantQuery,
            "Search",
            request.Query,
            "Success",
            $"{{\"query\":\"{request.Query}\",\"hitsCount\":{rankedHits.Count},\"elapsedMs\":{sw.ElapsedMilliseconds}}}",
            cancellationToken: cancellationToken);

        return new SearchResultDto
        {
            Query = request.Query,
            TotalHits = rankedHits.Count,
            Hits = rankedHits,
            ElapsedMilliseconds = sw.ElapsedMilliseconds
        };
    }

    private static double CalculateScore(string title, string description, string sectionHeader, string text, string fullQuery, List<string> terms)
    {
        double score = 0;

        var lowerTitle = title.ToLowerInvariant();
        var lowerDesc = description.ToLowerInvariant();
        var lowerHeader = sectionHeader.ToLowerInvariant();
        var lowerText = text.ToLowerInvariant();

        if (lowerTitle.Contains(fullQuery)) score += 6.0;
        if (lowerHeader.Contains(fullQuery)) score += 4.0;
        if (lowerText.Contains(fullQuery)) score += 3.0;

        foreach (var term in terms)
        {
            var stem = GetStem(term);

            if (lowerTitle.Contains(stem)) score += 3.0;
            if (lowerHeader.Contains(stem)) score += 2.0;
            if (lowerDesc.Contains(stem)) score += 1.5;

            var occurrences = CountOccurrences(lowerText, stem);
            if (occurrences > 0)
            {
                score += Math.Min(occurrences * 0.8, 4.0);
            }
        }

        return score;
    }

    private static string GetStem(string word)
    {
        if (word.Length <= 4) return word;
        return word.Substring(0, word.Length - 1);
    }

    private static int CountOccurrences(string source, string term)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(term, index, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            count++;
            index += term.Length;
        }
        return count;
    }

    private static string CreateSnippet(string text, List<string> terms)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        int firstIndex = -1;
        string matchedTerm = string.Empty;

        foreach (var term in terms)
        {
            var stem = GetStem(term);
            int idx = text.IndexOf(stem, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0 && (firstIndex == -1 || idx < firstIndex))
            {
                firstIndex = idx;
                matchedTerm = term;
            }
        }

        if (firstIndex == -1)
        {
            return text.Length > 200 ? text.Substring(0, 200) + "..." : text;
        }

        int start = Math.Max(0, firstIndex - 60);
        int end = Math.Min(text.Length, firstIndex + 140);

        var snippet = text.Substring(start, end - start).Trim();
        if (start > 0) snippet = "..." + snippet;
        if (end < text.Length) snippet += "...";

        return snippet;
    }

    async Task<IReadOnlyList<SecureKnowledgeBase.Domain.Interfaces.SearchResultItem>> SecureKnowledgeBase.Domain.Interfaces.ISearchService.SearchAsync(string query, AccessLevel userAccessLevel, int maxResults, CancellationToken cancellationToken)
    {
        var result = await SearchAsync(new SearchRequestDto { Query = query, MaxResults = maxResults }, userAccessLevel, cancellationToken);
        return result.Hits.Select(h => new SecureKnowledgeBase.Domain.Interfaces.SearchResultItem
        {
            DocumentId = h.DocumentId,
            DocumentTitle = h.DocumentTitle,
            Category = h.Category,
            AccessLevel = h.AccessLevel,
            ChunkId = h.ChunkId,
            ChunkNumber = h.ChunkNumber,
            ChunkText = h.Snippet,
            SectionHeader = h.SectionHeader,
            Score = h.Score
        }).ToList();
    }

    public Task IndexChunkAsync(SecureKnowledgeBase.Domain.Entities.DocumentChunk chunk, SecureKnowledgeBase.Domain.Entities.Document document, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task RemoveDocumentIndexAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

