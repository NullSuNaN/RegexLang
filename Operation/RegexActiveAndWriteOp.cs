using RegexLang.Code;
using RegexLang.Helper;
using RegexLang.Runtime;

namespace RegexLang.Operation;

/// <summary>
/// apply regexp to the active variable and write to another variable
/// </summary>
public record RegexActiveAndWriteOp(string VariableName, RegexReplacement Command, FileTraceInfo? FileTrace) : IOperation
{
  public async Task OperateAsync(TaskContext context)
  {
    string? value = context.ActiveValue;
    if (value == null)
    {
      context.Throw(new(RegexException.NoActiveValueException, "Regex replacement cannot operate null active value", FileTrace));
      return;
    }
    if (Command.Rule == null)
    {
      context.Throw(new(RegexException.ParsingException, "Illegal regular expression", FileTrace));
      return;
    }
    context.SetEffectiveValue(VariableName, Command.Apply(value, context));
  }
}