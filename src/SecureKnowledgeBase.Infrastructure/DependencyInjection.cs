using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecureKnowledgeBase.Application.Interfaces;
using SecureKnowledgeBase.Domain.Interfaces;
using SecureKnowledgeBase.Infrastructure.Configuration;
using SecureKnowledgeBase.Infrastructure.Persistence;
using SecureKnowledgeBase.Infrastructure.Repositories;
using SecureKnowledgeBase.Infrastructure.Services;
using SecureKnowledgeBase.Infrastructure.Services.Extractors;

namespace SecureKnowledgeBase.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SecurityOptions>(configuration.GetSection(SecurityOptions.SectionName));

        var dbPath = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app_data/secure_kb.db";
        var appDataDir = Path.Combine(Directory.GetCurrentDirectory(), "app_data");
        if (!Directory.Exists(appDataDir))
        {
            Directory.CreateDirectory(appDataDir);
        }

        var storageDir = Path.Combine(Directory.GetCurrentDirectory(), "app_data", "storage");
        if (!Directory.Exists(storageDir))
        {
            Directory.CreateDirectory(storageDir);
        }

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlite(dbPath);
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();

        services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IAuditService, AuditService>();

        services.AddSingleton<ITextExtractor>(sp => new CompositeTextExtractor(new ITextExtractor[]
        {
            new PlainTextExtractor(),
            new DocxTextExtractor(),
            new PdfTextExtractor()
        }));
        services.AddSingleton<ITextChunker, TextChunker>();
        services.AddScoped<IDocumentIndexingService, DocumentIndexingService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<SecureKnowledgeBase.Application.Interfaces.ISearchService, LocalSearchService>();
        services.AddScoped<SecureKnowledgeBase.Domain.Interfaces.ISearchService, LocalSearchService>();

        services.AddHttpClient<ILocalLlmClient, OllamaLlmClient>();
        services.AddSingleton<IExtractiveQaService, ExtractiveQaService>();
        services.AddScoped<IRagService, RagService>();

        services.AddScoped<IVulnerabilityLabService, VulnerabilityLabService>();
        services.AddSingleton<ISystemStatusService, SystemStatusService>();
        services.AddScoped<IAuthenticationService, LocalAuthenticationService>();
        services.AddScoped<CurrentUserService>();
        services.AddScoped<ICurrentUserService>(sp => sp.GetRequiredService<CurrentUserService>());

        return services;
    }
}
