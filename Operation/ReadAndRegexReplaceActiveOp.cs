using RegexLang.Code;
using RegexLang.Helper;
using RegexLang.Runtime;

namespace RegexLang.Operation;

/// <summary>
/// apply regexp to the active variable and write to another variable
/// </summary>
public record ReadAndRegexReplaceActiveOp(string VariableName, RegexReplacement Command, FileTraceInfo? FileTrace) : IOperation
{
  public async Task OperateAsync(TaskContext context)
  {
    string? value = context.QueryEffectiveValue(VariableName);
    if (value == null)
    {
      context.Throw(new(RegexException.NoActiveValueException, "Regex replacement cannot operate null target value", FileTrace));
      return;
    }
    if (Command.Rule == null)
    {
      context.Throw(new(RegexException.ParsingException, "Illegal regular expression", FileTrace));
      return;
    }
    context.ActiveValue = Command.Apply(value, context);
  }
}