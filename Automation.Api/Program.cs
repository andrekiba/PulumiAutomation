using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Automation.Api;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        await host.RunAsync();
    }

    static IHostBuilder CreateHostBuilder(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
            
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) => config.AddConfiguration(configuration))
            .ConfigureLogging((hostBuilderContext, loggingBuilder) =>
            {
                var logger = new LoggerConfiguration()
                    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}", theme: AnsiConsoleTheme.Code)
                    .MinimumLevel.Information()
                    .CreateLogger();

                loggingBuilder.AddSerilog(logger);
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
                webBuilder.UseConfiguration(configuration);
            })
            .UseSerilog();
    }
}