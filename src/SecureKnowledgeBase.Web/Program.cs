using System.Text.Json.Serialization;
using SecureKnowledgeBase.Infrastructure;
using SecureKnowledgeBase.Infrastructure.Persistence;
using SecureKnowledgeBase.Web.Components;

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 120;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("auth-limit", opt =>
    {
        opt.PermitLimit = 15;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbInitializer.InitializeAsync(dbContext);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.UseStaticFiles();

var reportsCandidate1 = Path.Combine(Directory.GetCurrentDirectory(), "reports");
var reportsCandidate2 = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "reports");
var resolvedReportsDir = Directory.Exists(reportsCandidate1) ? reportsCandidate1 : (Directory.Exists(reportsCandidate2) ? reportsCandidate2 : null);
if (resolvedReportsDir != null)
{
    var reportsContentTypeProvider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
    reportsContentTypeProvider.Mappings[".html"] = "text/html; charset=utf-8";
    reportsContentTypeProvider.Mappings[".json"] = "application/json; charset=utf-8";
    reportsContentTypeProvider.Mappings[".sarif"] = "application/json; charset=utf-8";
    reportsContentTypeProvider.Mappings[".log"] = "text/plain; charset=utf-8";
    reportsContentTypeProvider.Mappings[".sig"] = "text/plain; charset=utf-8";
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(Path.GetFullPath(resolvedReportsDir)),
        RequestPath = "/reports",
        ContentTypeProvider = reportsContentTypeProvider
    });
}

var docsCandidate1 = Path.Combine(Directory.GetCurrentDirectory(), "docs");
var docsCandidate2 = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "docs");
var resolvedDocsDir = Directory.Exists(docsCandidate1) ? docsCandidate1 : (Directory.Exists(docsCandidate2) ? docsCandidate2 : null);
if (resolvedDocsDir != null)
{
    var contentTypeProvider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
    contentTypeProvider.Mappings[".md"] = "text/plain; charset=utf-8";
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(Path.GetFullPath(resolvedDocsDir)),
        RequestPath = "/docs",
        ContentTypeProvider = contentTypeProvider
    });
}

app.UseAntiforgery();
app.UseRateLimiter();

app.MapControllers().RequireRateLimiting("fixed");
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
