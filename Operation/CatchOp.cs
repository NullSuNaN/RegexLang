using System.Text.RegularExpressions;
using RegexLang.Code;
using RegexLang.Runtime;

namespace RegexLang.Operation;

public record CatchOp(LinearOperationList TryBlock, LinearOperationList CatchBlock, Regex? Condition, string CatchAs, FileTraceInfo? FileTrace) : IOperation
{
  public async Task OperateAsync(TaskContext context)
  {
    if (Condition == null)
    {
      context.Throw(new(RegexException.ParsingException, "Failed to parse condition.", FileTrace));
      return;
    }
    await TryBlock.OperateAsync(context);
    var exception = context.QueryEffectiveValue(TaskContext.ExceptionTypeField);
    if(exception != null && Condition.IsMatch(exception))
    {
      context.SetEffectiveValue(TaskContext.ExceptionTypeField, null);
      context.SetEffectiveValue(CatchAs, exception);
      await CatchBlock.OperateAsync(context);
    }
  }
}