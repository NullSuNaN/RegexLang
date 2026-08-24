namespace RegexLang.Code;

public record FileTraceInfo(FileInfo File, int Line, int Column)
{
  public override string ToString()
  {
    string filePath;
    try { filePath = Path.GetRelativePath(Environment.CurrentDirectory, File.FullName); }
    catch
    {
      try { filePath = File.Name; } catch { filePath = "UNKNOWN"; };
    }
    return $"{filePath}:{Line}:{Column}";
  }
}