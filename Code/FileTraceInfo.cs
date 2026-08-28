using System.Text;

namespace RegexLang.Code;

public record FileTraceInfo(string FilePath, long? Line, long? Column)
{
  public FileTraceInfo(FileInfo File, long? Line, long? Column) : this(GetPath(File), Line, Column) { }
  public static string GetPath(FileInfo file)
  {
    try { return Path.GetRelativePath(Environment.CurrentDirectory, file.FullName); }
    catch
    {
      try { return file.Name; } catch { return "UNKNOWN"; }
      ;
    }
  }
  public override string ToString()
  {
    StringBuilder builder = new();
    builder.Append(FilePath);
    if(Line != null)
    {
      builder.Append(':');
      builder.Append(Line);
    }
    if(Column != null)
    {
      if(Line == null) builder.Append(":?");
      builder.Append(':');
      builder.Append(Column);
    }
    return builder.ToString();
  }
}