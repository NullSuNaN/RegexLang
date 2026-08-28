using RegexLang.Code.Compiling;
using RegexLang.Helper;
using RegexLang.Runtime;
using RegexLang.Operation;
using RegexLang.Code;

namespace RegexLang.Program.Shell;

internal static class RegexLangShell
{
  public static async Task<int> RunShellAsync(CancellationToken cancellationToken)
  {
    Action<ConsoleCancelEventArgs>? CancelSingleEvent = null, CancelDoubleEvent = null;
    Reference<DateTime?>[] lastInterruptions = [new(null), new(null)];
    Console.CancelKeyPress += (sender, e) =>
    {
      var now = DateTime.UtcNow;

      // Triple
      if (now - Interlocked.Exchange(ref lastInterruptions[1], lastInterruptions[0]).Value < TimeSpan.FromSeconds(1))
        return;

      e.Cancel = true;

      // Double
      if (now - Interlocked.Exchange(ref lastInterruptions[0], new(now)).Value < TimeSpan.FromSeconds(.5))
        CancelDoubleEvent?.Invoke(e);

      // Single
      CancelSingleEvent?.Invoke(e);
    };

    bool ended = false;
    try
    {
      Console.WriteLine($"RegexLang {Environment.Version}, {Environment.OSVersion}");

      CancellationTokenSource innerExitSource = new();
      CancellationTokenSource combinedSource = CancellationTokenSource.CreateLinkedTokenSource(innerExitSource.Token, cancellationToken);
      cancellationToken = combinedSource.Token;

      TaskContext context = new();
      cancellationToken.Register(context.cts.Cancel);
      context.StoredFunctions.Add("#shell.exit", new ExitShellOp(innerExitSource));

      CancelDoubleEvent = e =>
      {
        async Task ShowWarning()
        {
          await Task.Delay(TimeSpan.FromSeconds(2));
          if (!ended)
            Console.WriteLine("Not terminating, press ^C 3 times in 1.5s to force an interruption.");
        }
        innerExitSource.Cancel();
        _ = ShowWarning();
        return;
      };

      Console.WriteLine("You are now entering RegexLang Interactive Shell.");
      Console.WriteLine("Run f/c/.*/#shell.exit/ms or press ^C twice to exit.");
      Console.WriteLine("Press ^C 3 times in 1 seconds to force exit.");

      using ConsoleInputReader baseReader = new(cancellationToken: cancellationToken);
      using PositionTrackingTextReader reader = new(baseReader);

      CancellationTokenSource abortCompilingSource = new();

      CancelSingleEvent = e =>
      {
        context.cts.Cancel();
        abortCompilingSource.Cancel();
      };
      while (true)
      {
        cancellationToken.ThrowIfCancellationRequested();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("> ");
        Console.ResetColor();
        List<CompileOutput> compileOutputs = [];
        try
        {
          var op = await CodeCompiler.CompileOperationListAsync(
            new(new(new(compileOutputs), "(shell)", reader), CodeCompiler.EndOfBlock.EndOfOperation),
            CancellationTokenSource.CreateLinkedTokenSource(combinedSource.Token, abortCompilingSource.Token).Token
          );
          await compileOutputs.PrintCompileOutputAsync(Console.Out, false);
          if (op != null)
          {
            await op.OperateAsync(context);
            context.CheckException(new("(shell)", 1, 1));
            Console.WriteLine();
            context.PrintException();
            context.Catch();
          }
        }
        catch (OperationCanceledException) when (!combinedSource.IsCancellationRequested)
        {
        }
        abortCompilingSource = new();
        context.cts = new();
      }
    }
    catch (OperationCanceledException) { }
    finally { ended = true; }
    Console.WriteLine();

    return 0;
  }

  public record ExitShellOp(CancellationTokenSource CancellationTokenSource) : IOperation
  {
    public FileTraceInfo? FileTrace => null;

    public async Task OperateAsync(TaskContext context)
    {
      CancellationTokenSource.Cancel();
    }
  }
}