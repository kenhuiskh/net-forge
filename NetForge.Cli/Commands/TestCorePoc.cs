using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace NetForge.Cli.Commands;

public class TestCorePoc : Command<TestCorePoc.Settings>
{
    public sealed class Settings: CommandSettings
    {
        [CommandOption("-n|--name <NAME>")]
        [Description("The name to greet")]
        public string? Name { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine($"[bold blue]Running Core POC...[/]");
        AnsiConsole.MarkupLine($"[bold blue]Name: {settings.Name}[/]");

        return 0;
    }
}