using RegexLang.Code;
using RegexLang.Runtime;

namespace RegexLang.Operation;

public record DefineFunctionOp(LinearOperationList? OpList, string Name, FileTraceInfo? FileTrace) : IOperation
{
  public async Task OperateAsync(TaskContext context)
  {
    string? effectiveName = context.QueryEffectiveVariable(Name);
    if (effectiveName == null)
    {
      context.Throw(new(RegexException.NoFunctionNameException, "Function name points to null.", FileTrace));
      return;
    }
    if(OpList != null)
      context.StoredFunctions[effectiveName] = OpList;
    else
      context.StoredFunctions.Remove(effectiveName);
  }
}