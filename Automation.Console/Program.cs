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
            var workingDir = Path.Combine(executingDir, "..", "..", "..", "..", "Automation.Infra");
            
            var stackArgs = new LocalProgramArgs(stackName, workingDir);
            var stack = await LocalWorkspace.CreateOrSelectStackAsync(stackArgs);

            AnsiConsole.WriteLine("successfully initialized stack");

            // set stack configuration specifying the region to deploy
            AnsiConsole.WriteLine("setting up config...");
            await stack.SetConfigAsync("azure-native:location", new ConfigValue("westeurope"));
            await stack.SetConfigAsync("azure-native:subscriptionId", new ConfigValue("6b4ce01c-5368-4bb0-af54-be67444292c2"));
            AnsiConsole.WriteLine("config set");

            AnsiConsole.WriteLine("refreshing stack...");
            await stack.RefreshAsync(new RefreshOptions { OnStandardOutput = System.Console.WriteLine });
            AnsiConsole.WriteLine("refresh complete");

            if (destroy)
            {
                AnsiConsole.WriteLine("destroying stack...");
                await stack.DestroyAsync(new DestroyOptions { OnStandardOutput = System.Console.WriteLine });
                AnsiConsole.WriteLine("stack destroy complete");
            }
            else
            {
                AnsiConsole.WriteLine("updating stack...");
                var result = await stack.UpAsync(new UpOptions { OnStandardOutput = System.Console.WriteLine });

                if (result.Summary.ResourceChanges != null)
                {
                    AnsiConsole.WriteLine("update summary:");
                    foreach (var change in result.Summary.ResourceChanges)
                        AnsiConsole.WriteLine($"    {change.Key}: {change.Value}");
                }

                AnsiConsole.WriteLine($"primaryStorageKey: {result.Outputs["primaryStorageKey"].Value}");
            }
        }
    }
}