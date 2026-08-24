using RegexLang.Code;
using RegexLang.Helper;
using RegexLang.Runtime;

namespace RegexLang.Operation;

/// <summary>
/// A method for outputting current active value
/// </summary>
public record InputOp(RegexReplacement Command, FileTraceInfo? FileTrace) : IOperation
{
  public async Task OperateAsync(TaskContext context)
  {
    string? original = context.ActiveValue;
    if (original == null)
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
      var result = new char[original.Length];
      await Console.In.ReadAsync(result, 0, original.Length);
      context.ActiveValue = Command.Apply(string.Concat(result), context);
    }
    catch(IOException ex)
    {
      context.Throw(new(RegexException.IOException, ex.Message, FileTrace));
    }
  }
}