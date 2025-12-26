using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Spectre.Console.Cli;
using NetForge.Cli.Commands;
using NetForge.Cli.Infrastructure;
using NetForge.Core.Configuration;
using NetForge.Core.Interfaces;
using NetForge.Api.Services;
using DotNetEnv;

// Load .env from the API project directory
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

var services = new ServiceCollection();

// Add Configuration
var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

services.AddSingleton<IConfiguration>(configuration);

// Add Gemini Settings
services.Configure<GeminiSettings>(options =>
{
    options.ApiKey = configuration["GEMINI_API_KEY"] ?? string.Empty;
});

// Add HttpClient and GeminiClient
services.AddHttpClient<IGeminiClient, GeminiClient>();

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.SetApplicationName("NetForge CLI");

    config.AddCommand<TestCorePoc>("test-core")
            .WithDescription("Runs a POC test for NetForge.Core logic");

    config.AddCommand<ExpenseTracker>("expense-tracker")
            .WithDescription("Expense Tracker CLI version");
});

return await app.RunAsync(args);

