using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;

internal class ExampleHelpAction(HelpAction action, Func<ParseResult, string[]> getHelpList) : SynchronousCommandLineAction
{
    private readonly HelpAction _defaultHelp = action;

  public override int Invoke(ParseResult parseResult)
    {
        int result = _defaultHelp.Invoke(parseResult);
        Console.WriteLine("Examples:");
        foreach(var str in getHelpList(parseResult))
            Console.WriteLine($"  {str}");
        return result;

    }
}