using RegexLang.Code.Compiling;
using RegexLang.Program.Shell;
using System.CommandLine;
using System.CommandLine.Help;

// 1. Shared argument definition
var optionalFileArgument = new Argument<FileInfo>("filename") { Description = "The path to the target file.", Arity = ArgumentArity.ZeroOrOne };
var fileArgument = new Argument<FileInfo>("filename") { Description = "The path to the target file." };

// 2. Define subcommands
var runCommand = new Command("run", "Runs the specified file.") { fileArgument };
runCommand.SetAction(async (result, ct) => await HandleRunAsync(result.GetValue(fileArgument)!, ct));

var checkCommand = new Command("check", "Checks the specified file.") { fileArgument };
checkCommand.SetAction(async (result, ct) => await HandleCheckAsync(result.GetValue(fileArgument)!, ct));

var shellCommand = new Command("shell", "Opens the RegexLang interactive shell.");
shellCommand.SetAction(async (result, ct) => await HandleRunShellAsync(ct));

// 3. Root configuration
var rootCommand = new RootCommand("RegexLang execution tool");

// Add subcommands
rootCommand.Subcommands.Add(runCommand);
rootCommand.Subcommands.Add(checkCommand);
rootCommand.Subcommands.Add(shellCommand);

// To support 'RegexLang <filename>' natively without subtyping conflict, 
// add the filename argument directly to the root, but handle fallback execution:
rootCommand.Arguments.Add(optionalFileArgument);

rootCommand.SetAction(async (result, ct) =>
{
  var file = result.GetValue(optionalFileArgument);
  if (file != null)
  {
    return await HandleRunAsync(file, ct);
  }

  // If no file and no subcommand was passed, show information
  return await HandleRunShellAsync(ct);
});

foreach (var i in rootCommand.Options)
{
  if (i is HelpOption defaultHelpOption)
  {
    defaultHelpOption.Action = new ExampleHelpAction((HelpAction)defaultHelpOption.Action!, result =>
    {
      string processName = Path.GetFileName(Environment.ProcessPath) ?? "RegexLang";
      return result.CommandResult.Command switch
      {
        var value when value == runCommand => [$"{processName} run Example.rexl"],
        var value when value == checkCommand => [$"{processName} check Example.rexl"],
        var value when value == shellCommand => [$"{processName} shell"],
        _ => [
          $"{processName} Example.rexl # Run Example.rexl",
          $"{processName} -- Example.rexl # Run Example.rexl",
          $"{processName} run Example.rexl # Run Example.rexl",
          $"{processName} check Example.rexl # Check the syntax of Example.rexl",
          $"{processName} shell # open interactive shell"
        ],
      };
    });
  }
}

var config = new InvocationConfiguration 
{ 
    ProcessTerminationTimeout = null 
};

return await rootCommand.Parse(args).InvokeAsync(config);

async Task<int> HandleRunAsync(FileInfo entryFile, CancellationToken cancellationToken)
{
  var result = await CodeCompiler.CompileAsync(entryFile, cancellationToken);
  await result.Outputs.AsEnumerable().PrintCompileOutputAsync(Console.Out, signalNoErrors: false);
  if (result.Program == null) return 1;
  // Console.WriteLine(result.Program.Entry?.ToString() ?? "NO ENTRY");
  return await result.Program.RunAsync();
}

async Task<int> HandleCheckAsync(FileInfo entryFile, CancellationToken cancellationToken)
{
  var result = await CodeCompiler.CompileAsync(entryFile, cancellationToken);
  await result.Outputs.AsEnumerable().PrintCompileOutputAsync(Console.Out, signalNoErrors: true);
  if (result.Program == null) return 1;
  return 0;
}

Task<int> HandleRunShellAsync(CancellationToken cancellationToken) =>
  RegexLangShell.RunShellAsync(default);