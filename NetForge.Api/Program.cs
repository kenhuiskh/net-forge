using DotNetEnv;
using NetForge.Core.Configuration;
using NetForge.Core.Interfaces;
using NetForge.Api.Services;

var envPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
}
else
{
    // Fallback to default behavior if not found
    Env.Load();
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.AllowAnyMethod()
              .AllowAnyHeader()
              .WithOrigins("http://localhost:3000", "http://localhost:5002");
    });
});

builder.Services.Configure<GeminiSettings>(options =>
{
    options.ApiKey = builder.Configuration["GEMINI_API_KEY"] ?? string.Empty;
});
builder.Services.AddHttpClient<IGeminiClient, GeminiClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowLocalhost");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
