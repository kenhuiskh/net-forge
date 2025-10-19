using DotNetEnv;
using NetForge.Configuration;
using NetForge.Services;

Env.Load(); // Load .env into process env vars

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.Configure<GeminiSettings>(options =>
{
    options.ApiKey = builder.Configuration["GEMINI_API_KEY"] ?? string.Empty;
});
builder.Services.AddHttpClient<GeminiClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
