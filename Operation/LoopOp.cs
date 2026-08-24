using System.Text.RegularExpressions;
using RegexLang.Code;
using RegexLang.Runtime;

namespace RegexLang.Operation;

public record LoopOp(LinearOperationList OpList, Regex? Condition, FileTraceInfo? FileTrace) : IOperation
{
  public async Task OperateAsync(TaskContext context)
  {
    if (Condition == null)
    {
      context.Throw(new(RegexException.ParsingException, "Failed to parse condition.", FileTrace));
      return;
    }
    while (context.ActiveValue != null &&
          Condition.IsMatch(context.ActiveValue) && 
          context.QueryEffectiveValue(TaskContext.ExceptionTypeField) == null
    )
      await OpList.OperateAsync(context);
  }
}