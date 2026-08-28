using RegexLang.Code;
using RegexLang.Helper;
using RegexLang.Runtime;

namespace RegexLang.Operation;

/// <summary>
/// Call the function that matches both the name prefix and the regex.
/// </summary>
/// <param name="NamePrefix">Name prefix, with reference parsing</param>
public record CallFunctionOp(RegexReplacement NameRule, FileTraceInfo? FileTrace) : IOperation
{
  public async Task OperateAsync(TaskContext context)
  {
    string? value = context.ActiveValue;
    if (value == null)
    {
      context.Throw(new(RegexException.NoActiveValueException, "Regex replacement cannot operate null active value", FileTrace));
      return;
    }
    if (NameRule.Rule == null)
    {
      context.Throw(new(RegexException.ParsingException, "Illegal regular expression", FileTrace));
      return;
    }

    string? fnName = context.QueryEffectiveVariable(NameRule.Apply(value, context)!);
    if (fnName != null && context.StoredFunctions.TryGetValue(fnName, out var op))
      await op.OperateAsync(context);
    else
      context.Throw(new(RegexException.NoFunctionException, $"Function {fnName + ' '}is not defined.", FileTrace));
  }

}