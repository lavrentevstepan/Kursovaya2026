using Microsoft.Extensions.Options;
using SecureKnowledgeBase.Application.DTOs;
using SecureKnowledgeBase.Application.Interfaces;
using SecureKnowledgeBase.Infrastructure.Configuration;

namespace SecureKnowledgeBase.Infrastructure.Services;

public class SystemStatusService : ISystemStatusService
{
    private static readonly DateTime StartTimeUtc = DateTime.UtcNow;
    private readonly SecurityOptions _options;

    public SystemStatusService(IOptions<SecurityOptions> options)
    {
        _options = options.Value;
    }

    public Task<SystemStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var uptime = (DateTime.UtcNow - StartTimeUtc).TotalSeconds;
        var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var status = new SystemStatusDto
        {
            ApplicationName = "SecureKnowledgeBase (SKB)",
            Version = "1.0.0",
            Status = "Healthy",
            DatabaseProvider = _options.DatabaseProvider,
            SearchEngine = _options.SearchEngineProvider,
            IsLocalLlmEnabled = _options.EnableLocalLlm,
            Environment = envName,
            UptimeSeconds = Math.Round(uptime, 2),
            TimestampUtc = DateTime.UtcNow,
            Services = new Dictionary<string, string>
            {
                ["WebAPI"] = "Online",
                ["BlazorServer"] = "Online",
                ["DatabaseAdapter"] = _options.DatabaseProvider,
                ["SearchAdapter"] = _options.SearchEngineProvider,
                ["LocalLlm"] = _options.EnableLocalLlm ? "Configured" : "Disabled (Extractive Mode)",
                ["DemoVulnerabilities"] = _options.EnableDemoVulnerabilities ? "Active" : "Disabled (Secure Mode)"
            }
        };

        return Task.FromResult(status);
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
