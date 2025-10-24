using DotNetEnv;
using NetForge.Configuration;
using NetForge.Services;

Env.Load(); // Load .env into process env vars

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
builder.Services.AddHttpClient<GeminiClient>();

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
