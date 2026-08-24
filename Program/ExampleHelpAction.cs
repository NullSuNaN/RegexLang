using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;

internal class ExampleHelpAction(HelpAction action) : SynchronousCommandLineAction
{
    private readonly HelpAction _defaultHelp = action;

  public override int Invoke(ParseResult parseResult)
    {
        int result = _defaultHelp.Invoke(parseResult);
        string processName = Path.GetFileName(Environment.ProcessPath) ?? "RegexLang";
        Console.WriteLine("Examples:");
        Console.WriteLine($"  {processName} Example.rexl");
        Console.WriteLine($"  {processName} -- Example.rexl");
        Console.WriteLine($"  {processName} run Example.rexl");
        Console.WriteLine($"  {processName} check Example.rexl");
        return result;

    }
}