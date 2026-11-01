using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SecureKnowledgeBase.Application.DTOs;
using SecureKnowledgeBase.Application.Interfaces;
using SecureKnowledgeBase.Domain.Enums;
using SecureKnowledgeBase.Infrastructure.Configuration;

namespace SecureKnowledgeBase.Infrastructure.Services;

public class RagService : IRagService
{
    private readonly ISearchService _searchService;
    private readonly ILocalLlmClient _llmClient;
    private readonly IExtractiveQaService _extractiveQaService;
    private readonly IAuditService _auditService;
    private readonly SecurityOptions _options;

    public RagService(
        ISearchService searchService,
        ILocalLlmClient llmClient,
        IExtractiveQaService extractiveQaService,
        IAuditService auditService,
        IOptions<SecurityOptions> options)
    {
        _searchService = searchService;
        _llmClient = llmClient;
        _extractiveQaService = extractiveQaService;
        _auditService = auditService;
        _options = options.Value;
    }

    public async Task<RagQueryResponseDto> AskAsync(RagQueryRequestDto request, Guid userId, string username, AccessLevel userAccessLevel, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var safeQuestion = request?.Question?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(safeQuestion))
        {
            return new RagQueryResponseDto
            {
                Question = string.Empty,
                Answer = "Пожалуйста, введите вопрос для поиска по базе знаний.",
                ModeUsed = RagMode.ExtractiveOnly,
                HasFoundAnswer = false,
                ElapsedMilliseconds = sw.ElapsedMilliseconds,
                ModelName = "None"
            };
        }

        var maxSources = request!.MaxSources > 0 && request.MaxSources <= 10 ? request.MaxSources : 3;
        var searchRequest = new SearchRequestDto
        {
            Query = safeQuestion,
            Category = request.Category,
            MaxResults = maxSources
        };

        var searchResult = await _searchService.SearchAsync(searchRequest, userAccessLevel, cancellationToken);
        var sourceDtos = searchResult.Hits.Select(h => new RagSourceItemDto
        {
            ChunkId = h.ChunkId,
            DocumentId = h.DocumentId,
            DocumentTitle = h.DocumentTitle,
            SectionHeader = h.SectionHeader,
            AccessLevel = h.AccessLevel,
            RelevanceScore = h.Score,
            Snippet = h.Snippet,
            FullText = h.FullText
        }).ToList();

        RagQueryResponseDto response;

        if (sourceDtos.Count == 0)
        {
            sw.Stop();
            response = new RagQueryResponseDto
            {
                Question = safeQuestion,
                Answer = "Точный ответ в доступной базе знаний не найден.",
                ModeUsed = request.Mode,
                HasFoundAnswer = false,
                ElapsedMilliseconds = sw.ElapsedMilliseconds,
                ModelName = "No-Context",
                Sources = new List<RagSourceItemDto>()
            };
        }
        else if (request.Mode == RagMode.ExtractiveOnly)
        {
            sw.Stop();
            response = _extractiveQaService.SynthesizeAnswer(safeQuestion, sourceDtos, sw.ElapsedMilliseconds);
        }
        else if (request.Mode == RagMode.LlmOnly)
        {
            var isAvailable = await _llmClient.IsAvailableAsync(cancellationToken);
            if (!isAvailable)
            {
                throw new InvalidOperationException("Сервис локальной LLM недоступен или отключен.");
            }

            try
            {
                var llmAnswer = await _llmClient.GenerateAnswerAsync(safeQuestion, sourceDtos, cancellationToken);
                sw.Stop();
                var hasAnswer = !llmAnswer.Contains("не найден", StringComparison.OrdinalIgnoreCase);

                response = new RagQueryResponseDto
                {
                    Question = safeQuestion,
                    Answer = llmAnswer,
                    ModeUsed = RagMode.LlmOnly,
                    HasFoundAnswer = hasAnswer,
                    ElapsedMilliseconds = sw.ElapsedMilliseconds,
                    ModelName = _options.OllamaModel,
                    Sources = sourceDtos
                };
            }
            catch (Exception)
            {
                sw.Stop();
                throw;
            }
        }
        else
        {
            bool usedLlm = false;
            string? llmAnswer = null;

            if (_options.EnableLocalLlm)
            {
                var isAvailable = await _llmClient.IsAvailableAsync(cancellationToken);
                if (isAvailable)
                {
                    try
                    {
                        llmAnswer = await _llmClient.GenerateAnswerAsync(safeQuestion, sourceDtos, cancellationToken);
                        usedLlm = true;
                    }
                    catch
                    {
                        usedLlm = false;
                    }
                }
            }

            sw.Stop();

            if (usedLlm && !string.IsNullOrWhiteSpace(llmAnswer))
            {
                var hasAnswer = !llmAnswer.Contains("не найден", StringComparison.OrdinalIgnoreCase);
                response = new RagQueryResponseDto
                {
                    Question = safeQuestion,
                    Answer = llmAnswer,
                    ModeUsed = RagMode.LlmOnly,
                    HasFoundAnswer = hasAnswer,
                    ElapsedMilliseconds = sw.ElapsedMilliseconds,
                    ModelName = _options.OllamaModel,
                    Sources = sourceDtos
                };
            }
            else
            {
                response = _extractiveQaService.SynthesizeAnswer(safeQuestion, sourceDtos, sw.ElapsedMilliseconds);
            }
        }

        var auditDetails = JsonSerializer.Serialize(new
        {
            question = safeQuestion.Length > 100 ? safeQuestion.Substring(0, 100) + "..." : safeQuestion,
            mode = response.ModeUsed.ToString(),
            model = response.ModelName,
            hasAnswer = response.HasFoundAnswer,
            sourcesCount = response.Sources.Count,
            elapsedMs = response.ElapsedMilliseconds
        });

        await _auditService.LogAsync(
            username ?? "anonymous",
            AuditAction.AssistantQuery,
            "RAG-Assistant",
            userId == Guid.Empty ? string.Empty : userId.ToString(),
            response.HasFoundAnswer ? "Success" : "NotFound",
            auditDetails,
            cancellationToken: cancellationToken);

        return response;
    }

    public async Task<LlmStatusDto> GetLlmStatusAsync(CancellationToken cancellationToken = default)
    {
        return await _llmClient.GetStatusAsync(cancellationToken);
    }
}
