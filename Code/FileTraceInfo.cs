namespace RegexLang.Code;

public record FileTraceInfo(string FilePath, long Line, long Column)
{
  public FileTraceInfo(FileInfo File, long Line, long Column) : this(GetPath(File), Line, Column) { }
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
    return $"{FilePath}:{Line}:{Column}";
  }
}