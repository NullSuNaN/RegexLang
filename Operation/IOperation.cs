using RegexLang.Code;
using RegexLang.Runtime;

namespace RegexLang.Operation;

public interface IOperation
{
  public Task OperateAsync(TaskContext context);
  public FileTraceInfo? FileTrace {get;}
}