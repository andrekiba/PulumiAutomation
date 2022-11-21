// See https://aka.ms/new-console-template for more information
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

            "successfully initialized stack".WriteLineColor("royalblue1");

            // set stack configuration specifying the region to deploy
            "setting up config...".WriteLineColor("royalblue1");
            await stack.SetConfigAsync("azure-native:location", new ConfigValue("westeurope"));
            await stack.SetConfigAsync("azure-native:subscriptionId", new ConfigValue("6b4ce01c-5368-4bb0-af54-be67444292c2"));
            "config set".WriteLineColor("royalblue1");
            
            var pulumi = AnsiConsole.Create(new AnsiConsoleSettings
            {
                
            });
            
            "refreshing stack...".WriteLineColor("royalblue1");
            await stack.RefreshAsync(new RefreshOptions { OnStandardOutput = pulumi.WriteLine });
            "refresh complete".WriteLineColor("royalblue1");

            if (destroy)
            {
                "destroying stack...".WriteLineColor("royalblue1");
                await stack.DestroyAsync(new DestroyOptions { OnStandardOutput = pulumi.WriteLine });
                "stack destroy complete".WriteLineColor("royalblue1");
            }
            else
            {
                "updating stack...".WriteLineColor("royalblue1");
                var result = await stack.UpAsync(new UpOptions { OnStandardOutput = pulumi.WriteLine });

                if (result.Summary.ResourceChanges != null)
                {
                    "update summary:".WriteLineColor("royalblue1");
                    foreach (var change in result.Summary.ResourceChanges)
                        $"{change.Key}: {change.Value}".WriteLineColor("royalblue1");
                }
                
                AnsiConsole.WriteLine($"staticEndpoint: {result.Outputs["staticEndpoint"].Value}");
                AnsiConsole.WriteLine($"primaryStorageKey: {result.Outputs["primaryStorageKey"].Value}");
            }
        }
    }
}