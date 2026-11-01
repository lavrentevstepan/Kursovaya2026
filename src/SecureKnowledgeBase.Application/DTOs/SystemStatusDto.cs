namespace SecureKnowledgeBase.Application.DTOs;

public class SystemStatusDto
{
    public string ApplicationName { get; set; } = "SecureKnowledgeBase";
    public string Version { get; set; } = "1.0.0";
    public string Status { get; set; } = "Healthy";
    public string DatabaseProvider { get; set; } = "SQLite";
    public string SearchEngine { get; set; } = "LocalFullText";
    public bool IsLocalLlmEnabled { get; set; }
    public string Environment { get; set; } = "Development";
    public double UptimeSeconds { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public IDictionary<string, string> Services { get; set; } = new Dictionary<string, string>();
}
