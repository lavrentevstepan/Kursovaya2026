using Microsoft.Extensions.Options;
using SecureKnowledgeBase.Infrastructure.Configuration;
using SecureKnowledgeBase.Infrastructure.Services;
using Xunit;

namespace SecureKnowledgeBase.UnitTests.Services;

public class SystemStatusServiceTests
{
    [Fact]
    public async Task GetStatusAsync_ShouldReturnHealthyStatusWithDefaults()
    {
        var options = Options.Create(new SecurityOptions
        {
            DatabaseProvider = "SQLite",
            SearchEngineProvider = "LocalFullText",
            EnableLocalLlm = false
        });

        var service = new SystemStatusService(options);
        var result = await service.GetStatusAsync();

        Assert.NotNull(result);
        Assert.Equal("Healthy", result.Status);
        Assert.Equal("SQLite", result.DatabaseProvider);
        Assert.Equal("LocalFullText", result.SearchEngine);
        Assert.False(result.IsLocalLlmEnabled);
        Assert.Contains("WebAPI", result.Services.Keys);
        Assert.Contains("BlazorServer", result.Services.Keys);
    }

    [Fact]
    public async Task IsHealthyAsync_ShouldReturnTrue()
    {
        var options = Options.Create(new SecurityOptions());
        var service = new SystemStatusService(options);
        var isHealthy = await service.IsHealthyAsync();

        Assert.True(isHealthy);
    }
}
