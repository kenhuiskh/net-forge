using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using DotNetEnv;
using NetForge.Core.Data;

namespace NetForge.Cli;

public class NetForgeDbContextFactory : IDesignTimeDbContextFactory<NetForgeDbContext>
{
    public NetForgeDbContext CreateDbContext(string[] args)
    {
        // 1. Load the .env file (it's in the root folder, 4 levels up from /bin/Debug/...)
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
        }
        // 2. Build configuration
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();
        // 3. Setup Options
        var optionsBuilder = new DbContextOptionsBuilder<NetForgeDbContext>();
        var connectionString = configuration["DB_CONNECTION_STRING"];
        
        optionsBuilder.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
        return new NetForgeDbContext(optionsBuilder.Options);
    }
}