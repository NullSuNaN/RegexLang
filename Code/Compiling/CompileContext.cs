using RegexLang.Helper;
using static RegexLang.Code.Compiling.CodeCompiler;

namespace RegexLang.Code.Compiling;

public class CompileContext(CompileContext.Part.FileContext FileContext, EndOfBlock EOB)
{
  public static class Part
  {
    public class FileContext(GlobalContext GlobalContext, string FilePath, PositionTrackingTextReader CodeReader)
    {
      public GlobalContext GlobalContext = GlobalContext;
      public string FilePath = FilePath;
      public PositionTrackingTextReader CodeReader = CodeReader;
    }
    public class GlobalContext(List<CompileOutput> CompileOutputs)
    {
      public List<CompileOutput> CompileOutputs = CompileOutputs; 
    }
  }
  public List<CompileOutput> CompileOutputs { get => FileContext.GlobalContext.CompileOutputs; set => FileContext.GlobalContext.CompileOutputs = value; }
  public Part.FileContext FileContext = FileContext;
  public string FilePath { get => FileContext.FilePath; set => FileContext.FilePath = value; }
  public PositionTrackingTextReader CodeReader { get => FileContext.CodeReader; set => FileContext.CodeReader = value; }
  public EndOfBlock EOB = EOB;


  public CompileContext(CompileContext original): this(original.FileContext, original.EOB) {}
  public CompileContext Fork() => new(this);
}