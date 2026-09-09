using Microsoft.EntityFrameworkCore;
using RagPlatform.Api.Infrastructure;
using RagPlatform.Application.Common.Interfaces;
using RagPlatform.Infrastructure;
using RagPlatform.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ---- Services -----------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Enterprise RAG & Document Intelligence API",
        Version = "v1",
        Description = "On-prem Retrieval-Augmented Generation platform: ingest PDFs, " +
                      "run semantic search over a vector store and chat with your documents."
    });
});

builder.Services.AddInfrastructure(builder.Configuration);

// Allow large PDF uploads (default multipart limit is ~128 MB; raise to 200 MB).
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 200L * 1024 * 1024;
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 200L * 1024 * 1024;
});

var corsPolicy = "frontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicy, policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// ---- Startup initialisation (retry until dependencies are ready) --------
await StartupInitializer.InitializeAsync(app);

// ---- Middleware ---------------------------------------------------------
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "RAG Platform API v1");
    options.DocumentTitle = "Enterprise RAG Platform — API";
});

app.UseCors(corsPolicy);
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
