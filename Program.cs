using RegexLang.Code;
using RegexLang.Code.Compiling;
using RegexLang.Operation;
using RegexLang.Runtime;
using System.CommandLine;
using System.CommandLine.Help;

// 1. Shared argument definition
var fileArgument = new Argument<FileInfo>("filename") { Description = "The path to the target file.", Arity = ArgumentArity.ZeroOrOne };

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
rootCommand.Arguments.Add(fileArgument);

rootCommand.SetAction(async (result, ct) =>
{
  var file = result.GetValue(fileArgument);
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
    defaultHelpOption.Action = new ExampleHelpAction((HelpAction)defaultHelpOption.Action!);
  }
}

return await rootCommand.Parse(args).InvokeAsync();

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

async Task<int> HandleRunShellAsync(CancellationToken cancellationToken)
{
  CancellationTokenSource innerExitSource = new();
  DateTime? lastInterruption = null;
  Console.CancelKeyPress += (sender, e) =>
  {
    var now = DateTime.UtcNow;
    if(now - Interlocked.Exchange(ref lastInterruption, now) < TimeSpan.FromSeconds(1))
      return;
    e.Cancel = true;
    innerExitSource.Cancel();
  };
  Console.WriteLine($"RegexLang {Environment.Version}, {Environment.OSVersion}");
  Console.WriteLine("You are now entering RegexLang Console CLI.");
  Console.WriteLine("Run f/c/#shell.exit/ or press ^C to exit.");
  Console.WriteLine("Press ^C twice in 1 second to force exit.");
  Console.WriteLine();
  Console.WriteLine("The feature is not available in this version.");

  return 0;
}

record ExitShellOp(CancellationTokenSource Cts) : IOperation
{
  public FileTraceInfo? FileTrace => null;

  public async Task OperateAsync(TaskContext context)
  {
    Cts.Cancel();
  }
}