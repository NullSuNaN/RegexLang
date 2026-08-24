using System.Security.Cryptography;
using System.Text.RegularExpressions;
using RegexLang.Code;
using RegexLang.Runtime;

namespace RegexLang.Operation;

/// <summary>
/// Call the function that matches both the name prefix and the regex.
/// </summary>
/// <param name="NamePrefix">Name prefix, with reference parsing</param>
public record CallFunctionOp(string Name, FileTraceInfo? FileTrace) : IOperation
{
  public async Task OperateAsync(TaskContext context)
  {
    string? effectiveName = context.QueryEffectiveVariable(Name);
    if (effectiveName == null)
    {
      context.Throw(new(RegexException.NoFunctionNameException, "Function name points to null.", FileTrace));
      return;
    }
    if (context.StoredFunctions.TryGetValue(effectiveName, out var op))
      await op.OperateAsync(context);
    else
      context.Throw(new(RegexException.NoFunctionException, "Function is not defined.", FileTrace));
  }
}