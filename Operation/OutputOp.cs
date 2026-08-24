using RegexLang.Code;
using RegexLang.Helper;
using RegexLang.Runtime;

namespace RegexLang.Operation;

/// <summary>
/// A method for outputting current active value
/// </summary>
public record OutputOp(RegexReplacement Command, FileTraceInfo? FileTrace) : IOperation
{
  public async Task OperateAsync(TaskContext context)
  {
    string? value = context.ActiveValue;
    if (value == null)
    {
      context.Throw(new(RegexException.NoActiveValueException, "Cannot output with null active value", FileTrace));
      return;
    }
    if (Command.Rule == null)
    {
      context.Throw(new(RegexException.ParsingException, "Illegal output command", FileTrace));
      return;
    }
    try
    {
      var result = Command.Apply(value, context);
      Console.Write(result);
    }
    catch(IOException ex)
    {
      context.Throw(new(RegexException.IOException, ex.Message, FileTrace));
    }
  }
}