using System.Text;

namespace RegexLang.Code.Compiling;

public record CompileOutput(CompileOutput.Levels Level, string Message, FileTraceInfo? FileTrace)
{
  public enum Levels
  {
    Info,
    Warning,
    Error
  }
  public override string ToString()
  {
    return $"{Level}: {FileTrace}: {Message}";
  }
}

public static class CompileOutputIEnumerableExtensions
{
  public static async Task PrintCompileOutputAsync(this IEnumerable<CompileOutput> outputs, TextWriter writer, bool signalNoErrors)
  {
    if (!outputs.Any())
    {
      if(signalNoErrors) await writer.WriteLineAsync("No Problems.");
    }
    else
    {
      Dictionary<CompileOutput.Levels, int> counters = [];
      StringBuilder result = new();
      foreach (var output in outputs)
      {
        result.AppendLine(output.ToString());
        counters[output.Level] = 1 + counters.GetValueOrDefault(output.Level, 0);
      }
      result.AppendLine(string.Join(", ", counters.Select(kvp => $"{kvp.Key}: {kvp.Value}")));
      await writer.WriteAsync(result.ToString());
    }
    await writer.FlushAsync();
  }
}