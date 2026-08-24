using RegexLang.Code;
using RegexLang.Runtime;

namespace RegexLang.Operation;

/// <summary>
/// A method to spawn new tasks
/// </summary>
public record TaskControlOp(FileTraceInfo? FileTrace) : IOperation
{
  public Task OperateAsync(TaskContext context)
  {
    throw new NotImplementedException();
  }
}