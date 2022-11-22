using Spectre.Console;

namespace Automation.CLI;

public static class Extensions
{
    public static void WriteLineColor(this string s, string color = "royalblue1")
    {
        AnsiConsole.MarkupLine($"[{color}]{s}[/]");
    }
}