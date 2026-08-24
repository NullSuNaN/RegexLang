using System.Text.RegularExpressions;
using RegexLang.Helper;
using RegexLang.Operation;
using RegexLang.Runtime;

namespace RegexLang.Code.Compiling;

public static class CodeCompiler
{
  public static async Task<(CompiledProgram? Program, List<CompileOutput> Outputs)> CompileAsync(FileInfo EntryFile, CancellationToken token)
  {
    using var stream = EntryFile.OpenRead();
    using StreamReader baseReader = new(stream);
    using PositionTrackingTextReader reader = new(baseReader);
    List<CompileOutput> outputs = [];
    CompiledProgram program = new()
    {
      Entry = await CompileOperationListAsync(EntryFile, reader, outputs, EndOfBlock.EndOfStream, token)
    };
    return (program.Entry != null ? program : null, outputs);
  }
  private const string WarnIllegalRegexRule = "Illegal regex rule, this will throw a parse exception";
  public static async Task<LinearOperationList?> CompileOperationListAsync(FileInfo file, PositionTrackingTextReader reader, List<CompileOutput> outputs, EndOfBlock eob, CancellationToken token)
  {
    LinearOperationList opList = new();
    bool success = true, ended = false;
    char[] OpCharBuffer = new char[1];
    FileTraceInfo GetTraceInfo() => new(file, reader.Line, reader.Column);
    async Task<RegexReplacement> ReadRegexReplacementAsync(char? setDelimiter = null)
    {
      var originalTraceInfo = GetTraceInfo();
      var replacement = await RegexReplacement.ParseRegexStreamAsync(reader, cancellationToken: token, setDelimiter: setDelimiter);
      if (replacement.Rule == null)
        outputs.Add(new(CompileOutput.Levels.Warning, WarnIllegalRegexRule, originalTraceInfo));
      return replacement;
    }
    while (!ended)
    {
      FileTraceInfo traceInfo = GetTraceInfo();
      if (await reader.ReadBlockAsync(OpCharBuffer, index: 0, count: 1) < 1)
      {
        if (eob == EndOfBlock.EndOfStream) break;
        outputs.Add(new(CompileOutput.Levels.Error, $"Missing ending {eob.ToString().ToLower()}", traceInfo));
        success = false;
        break;
      }
      char OpChar = OpCharBuffer[0];
      switch (OpChar)
      {
        case ' ':
        case '\t':
        case '\r':
        case '\n':
        case ';':
          break;
        case 's': // regex
          try { opList.Operations.Add(new RegexReplaceActiveOp(await ReadRegexReplacementAsync(), traceInfo)); }
          catch (FormatException ex) { success = false; outputs.Add(new(CompileOutput.Levels.Error, ex.Message, traceInfo)); }
          break;
        case 'r': // read and regex
          try
          {
            if (await reader.ReadAsync(OpCharBuffer, token) < 1)
              throw new FormatException("Missing delimiter.");
            char delimiter = OpCharBuffer[0];
            string targetVar = await RegexReplacement.ReadStringAsync(reader, delimiter, token);
            opList.Operations.Add(new ReadAndRegexReplaceActiveOp(targetVar, await ReadRegexReplacementAsync(setDelimiter: delimiter), traceInfo));
          }
          catch (FormatException ex) { success = false; outputs.Add(new(CompileOutput.Levels.Error, ex.Message, traceInfo)); }
          break;
        case 'w': // regex and write
          try
          {
            if (await reader.ReadAsync(OpCharBuffer, token) < 1)
              throw new FormatException("Missing delimiter.");
            char delimiter = OpCharBuffer[0];
            string targetVar = await RegexReplacement.ReadStringAsync(reader, delimiter, token);
            opList.Operations.Add(new RegexActiveAndWriteOp(targetVar, await ReadRegexReplacementAsync(setDelimiter: delimiter), traceInfo));
          }
          catch (FormatException ex) { success = false; outputs.Add(new(CompileOutput.Levels.Error, ex.Message, traceInfo)); }
          break;
        case 'v': // regex with dynamic replacement
          try { opList.Operations.Add(new RegexReplaceActiveDynamicReplaceOp(await ReadRegexReplacementAsync(), traceInfo)); }
          catch (FormatException ex) { success = false; outputs.Add(new(CompileOutput.Levels.Error, ex.Message, traceInfo)); }
          break;
        case 'i': // input
          try { opList.Operations.Add(new InputOp(await ReadRegexReplacementAsync(), traceInfo)); }
          catch (FormatException ex) { success = false; outputs.Add(new(CompileOutput.Levels.Error, ex.Message, traceInfo)); }
          break;
        case 'o': // output
          try { opList.Operations.Add(new OutputOp(await ReadRegexReplacementAsync(), traceInfo)); }
          catch (FormatException ex) { success = false; outputs.Add(new(CompileOutput.Levels.Error, ex.Message, traceInfo)); }
          break;
        case '#': // comment
          await reader.ReadLineAsync(token);
          break;
        case 'l': // loop
          try
          {
            if (await reader.ReadAsync(OpCharBuffer, token) < 1)
              throw new FormatException("Missing delimiter.");
            char delimiter = OpCharBuffer[0];
            string condition = await RegexReplacement.ReadStringAsync(reader, delimiter, token);
            string flags = await RegexReplacement.ReadStringAsync(reader, delimiter, token);
            var options = RegexReplacement.GetOptions(flags).Options;
            LinearOperationList? loopOpList = await CompileOperationListAsync(file, reader, outputs, EndOfBlock.Slash, token);
            if (loopOpList == null)
              success = false;
            else
            {
              Regex? expression = null;
              try { expression = new(condition, options, matchTimeout: Regex.InfiniteMatchTimeout); }
              catch (ArgumentException)
              {
                outputs.Add(new(CompileOutput.Levels.Warning, WarnIllegalRegexRule, traceInfo));
              }
              opList.Operations.Add(new LoopOp(loopOpList, expression, traceInfo));
            }
          }
          catch (FormatException ex) { success = false; outputs.Add(new(CompileOutput.Levels.Error, ex.Message, traceInfo)); }
          break;
        case '\\': // LinearOperationList
          var list = await CompileOperationListAsync(file, reader, outputs, EndOfBlock.Slash, token);
          if (list == null) break;
          opList.Operations.Add(list);
          break;
        case 'c': // try-catch
          try
          {
            if (await reader.ReadAsync(OpCharBuffer, token) < 1)
              throw new FormatException("Missing delimiter.");
            char delimiter = OpCharBuffer[0];
            string condition = await RegexReplacement.ReadStringAsync(reader, delimiter, token);
            string flags = await RegexReplacement.ReadStringAsync(reader, delimiter, token);
            var options = RegexReplacement.GetOptions(flags).Options;
            LinearOperationList? tryBlock = await CompileOperationListAsync(file, reader, outputs, EndOfBlock.Slash, token);
            string catchAs = await RegexReplacement.ReadStringAsync(reader, delimiter, token);
            LinearOperationList? catchBlock = await CompileOperationListAsync(file, reader, outputs, EndOfBlock.Slash, token);
            if (tryBlock == null || catchBlock == null)
              success = false;
            else
            {
              Regex? expression = null;
              try { expression = new(condition, options, matchTimeout: Regex.InfiniteMatchTimeout); }
              catch (ArgumentException)
              {
                outputs.Add(new(CompileOutput.Levels.Warning, WarnIllegalRegexRule, traceInfo));
              }
              opList.Operations.Add(new CatchOp(tryBlock, catchBlock, expression, catchAs, traceInfo));
            }
          }
          catch (FormatException ex) { success = false; outputs.Add(new(CompileOutput.Levels.Error, ex.Message, traceInfo)); }
          break;
        case 'f': // function
          try
          {
            if (await reader.ReadAsync(OpCharBuffer, token) < 1)
              throw new FormatException("Missing delimiter.");
            char delimiter = OpCharBuffer[0];
            string op = await RegexReplacement.ReadStringAsync(reader, delimiter, token);
            string name = await RegexReplacement.ReadStringAsync(reader, delimiter, token);
            switch (op)
            {
              case "s": // define
                LinearOperationList? loopOpList = await CompileOperationListAsync(file, reader, outputs, EndOfBlock.Slash, token);
                if (loopOpList == null)
                  success = false;
                else
                {
                  opList.Operations.Add(new DefineFunctionOp(loopOpList, name, traceInfo));
                }
                break;
              case "u": // remove
                opList.Operations.Add(new DefineFunctionOp(null, name, traceInfo));
                break;
              case "c": // call
                opList.Operations.Add(new CallFunctionOp(name, traceInfo));
                break;
              default:
                success = false; outputs.Add(new(CompileOutput.Levels.Error, $"Illegal function operation: {op}", traceInfo));
                break;
            }
          }
          catch (FormatException ex) { success = false; outputs.Add(new(CompileOutput.Levels.Error, ex.Message, traceInfo)); }
          break;
        case '/': // comment or end of block
          switch (reader.Peek())
          {
            case '/':
              await reader.ReadLineAsync(token);
              break;
            case '*':
              try { if (!(await reader.ReadUntilStringAsync("*/", token)).AsSpan()[^2..].SequenceEqual("*/")) throw new Exception(); }
              catch { outputs.Add(new(CompileOutput.Levels.Warning, "Comment not closed", GetTraceInfo())); }
              break;
            default:
              if (eob == EndOfBlock.Slash)
              {
                ended = true;
                break;
              }
              outputs.Add(new(CompileOutput.Levels.Warning, "Use #, // or /* */, or use \\ if you are starting a block", GetTraceInfo()));
              await reader.ReadLineAsync(token);
              break;
          }
          break;
        default:
          success = false;
          outputs.Add(new(CompileOutput.Levels.Error, $"Illegal operation: {OpChar}", GetTraceInfo()));
          break;
      }
    }
    return success ? opList : null;
  }

  public enum EndOfBlock
  {
    /// <summary>
    /// root level, end of stream
    /// </summary>
    EndOfStream,
    /// <summary>
    /// In a block, <c>/</c> ending
    /// </summary>
    Slash
  };
}