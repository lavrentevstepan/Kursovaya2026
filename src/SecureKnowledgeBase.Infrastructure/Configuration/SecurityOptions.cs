namespace SecureKnowledgeBase.Infrastructure.Configuration;

public class SecurityOptions
{
    public const string SectionName = "Security";

    public bool EnableDemoVulnerabilities { get; set; } = false;
    public int MaxFileSizeMb { get; set; } = 20;
    public string[] AllowedExtensions { get; set; } = [".txt", ".md", ".pdf", ".docx"];
    public string StorageDirectory { get; set; } = "app_data/storage";
    public string DatabaseProvider { get; set; } = "SQLite";
    public string SearchEngineProvider { get; set; } = "LocalFullText";
    public bool EnableLocalLlm { get; set; } = false;
    public string OllamaEndpoint { get; set; } = "http://localhost:11434";
    public string OllamaModel { get; set; } = "qwen2.5:1.5b";
    public int LlmTimeoutSeconds { get; set; } = 10;
}
