using RegexLang.Code;
using RegexLang.Helper;
using RegexLang.Runtime;

namespace RegexLang.Operation;

public record RegexReplaceActiveOp(RegexReplacement Command, FileTraceInfo? FileTrace) : IOperation
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
      context.Throw(new(RegexException.ParsingException, "Illegal sed rule", FileTrace));
      return;
    }
    context.ActiveValue = Command.Apply(value, context);
  }
}