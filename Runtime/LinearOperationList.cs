using System.Text;
using RegexLang.Code;
using RegexLang.Operation;

namespace RegexLang.Runtime;

public class LinearOperationList : IOperation
{
  public List<IOperation> Operations = [];

  public FileTraceInfo? FileTrace => Operations.AsEnumerable().FirstOrDefault((IOperation?)null)?.FileTrace;

  public async Task OperateAsync(TaskContext context)
  {
    foreach (var op in Operations)
    {
      await op.OperateAsync(context);
      // Console.WriteLine($"Operating {op.GetType()} {op.FileTrace}");
      if(context.CheckException(op.FileTrace))
        break;
    }
  }

  public override string ToString()
  {
    StringBuilder builder = new();
    builder.AppendLine("Linear");
    foreach (var op in Operations)
      builder.AppendLine(op.ToString());
    return builder.ToString();
  }
}