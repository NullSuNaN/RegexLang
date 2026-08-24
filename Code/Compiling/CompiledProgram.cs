using RegexLang.Helper;
using RegexLang.Runtime;

namespace RegexLang.Code.Compiling;

public class CompiledProgram
{

  public readonly TrieStringDictionary<LinearOperationList> Methods = [];

  public LinearOperationList? Entry;

  public async Task<int> RunAsync()
  {
    TaskContext context = new();
    await (Entry?.OperateAsync(context) ?? Task.CompletedTask);
    if (context.PrintException()) return 1;
    string? exitValue = context.QueryEffectiveValue(TaskContext.BinaryExitValueField);
    if (exitValue == null) return 0;
    try
    {
      return Convert.ToInt32(exitValue, 2);
    }
    catch
    {
      context.Throw(new(RegexException.NumberParsingException, "Illegal Exit Value", null));
      context.PrintException();
      return 1;
    }
  }
  public static implicit operator Func<Task<int>>(CompiledProgram program) => program.RunAsync;
}