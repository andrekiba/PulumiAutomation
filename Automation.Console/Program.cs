// to destroy our program, we can run "dotnet run destroy"

using System.Reflection;
using Pulumi.Automation;
using Spectre.Console;

namespace Automation.Console
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            // to destroy our program, we can run "dotnet run destroy"
            var destroy = args.Any() && args[0] == "destroy";

            const string stackName = "main";

            // need to account for the assembly executing from within the bin directory
            // when getting path to the local program
            var executingDir = new DirectoryInfo(Assembly.GetExecutingAssembly().Location).Parent?.FullName;
            var workingDir = Path.Combine(executingDir!, "..", "..", "..", "..", "Automation.Infra");
            
            var stackArgs = new LocalProgramArgs(stackName, workingDir);
            var stack = await LocalWorkspace.CreateOrSelectStackAsync(stackArgs);

            "successfully initialized stack".WriteLineColor();

            // set stack configuration specifying the region to deploy
            "setting up config...".WriteLineColor();
            await stack.SetConfigAsync("azure-native:location", new ConfigValue("westeurope"));
            await stack.SetConfigAsync("azure-native:subscriptionId", new ConfigValue("6b4ce01c-5368-4bb0-af54-be67444292c2"));
            "config set".WriteLineColor();

            "refreshing stack...".WriteLineColor();
            await stack.RefreshAsync(new RefreshOptions { OnStandardOutput = AnsiConsole.WriteLine });
            "refresh complete".WriteLineColor();

            if (destroy)
            {
                "destroying stack...".WriteLineColor();
                await stack.DestroyAsync(new DestroyOptions { OnStandardOutput = AnsiConsole.WriteLine });
                "stack destroy complete".WriteLineColor();
            }
            else
            {
                "updating stack...".WriteLineColor();
                var result = await stack.UpAsync(new UpOptions { OnStandardOutput = AnsiConsole.WriteLine });

                if (result.Summary.ResourceChanges != null)
                {
                    "update summary:".WriteLineColor();
                    foreach (var change in result.Summary.ResourceChanges)
                        $"{change.Key}: {change.Value}".WriteLineColor();
                }
                
                AnsiConsole.WriteLine($"staticEndpoint: {result.Outputs["staticEndpoint"].Value}");
                AnsiConsole.WriteLine($"primaryStorageKey: {result.Outputs["primaryStorageKey"].Value}");
            }
        }
    }
}