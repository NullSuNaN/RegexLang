using System.Text.RegularExpressions;
using RegexLang.Helper;
using RegexLang.Operation;
using RegexLang.Runtime;

namespace RegexLang.Code.Compiling;

public static class CodeCompiler
{
  public static async Task<(CompiledProgram? Program, List<CompileOutput> Outputs)> CompileAsync(FileInfo EntryFile, CancellationToken token)
  {
    List<CompileOutput> outputs = [];
    FileStream stream;
    try
    {
      stream = EntryFile.OpenRead();
    }
    catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
    {
      outputs.Add(new(CompileOutput.Levels.Error, ex.Message, new(EntryFile, null, null)));
      return (null, outputs);
    }
    using var _ = stream;
    using StreamReader baseReader = new(stream);
    using PositionTrackingTextReader reader = new(baseReader);
    CompiledProgram program = new()
    {
      Entry = await CompileOperationListAsync(new(new(new(outputs), FileTraceInfo.GetPath(EntryFile), reader), EndOfBlock.EndOfStream), token)
    };
    return (program.Entry != null ? program : null, outputs);
  }
  private const string WarnIllegalRegexRule = "Illegal regex rule, this will throw a parse exception";
  public static async Task<LinearOperationList?> CompileOperationListAsync(CompileContext context, CancellationToken token)
  {
    var reader = context.CodeReader;
    LinearOperationList opList = new();
    bool success = true, ended = false;
    char[] OpCharBuffer = new char[1];
    FileTraceInfo GetTraceInfo() => new(context.FilePath, reader.Line, reader.Column);
    async Task<RegexReplacement> ReadRegexReplacementAsync(char? setDelimiter = null)
    {
      var originalTraceInfo = GetTraceInfo();
      var replacement = await RegexReplacement.ParseRegexStreamAsync(reader, cancellationToken: token, setDelimiter: setDelimiter);
      if (replacement.Rule == null)
        context.CompileOutputs.Add(new(CompileOutput.Levels.Warning, WarnIllegalRegexRule, originalTraceInfo));
      return replacement;
    }
    char? OpChar = null;
    while (!ended)
    {
      FileTraceInfo traceInfo = GetTraceInfo();
      if (OpChar == null)
      {
        if (await reader.ReadBlockAsync(OpCharBuffer, cancellationToken: token) < 1)
        {
          if (context.EOB == EndOfBlock.EndOfStream) break;
          context.CompileOutputs.Add(new(CompileOutput.Levels.Error, $"Missing ending {context.EOB.ToString().ToLower()}", traceInfo));
          success = false;
          break;
        }
        OpChar = OpCharBuffer[0];
      }
      var _op = OpChar;
      OpChar = null;
      switch (_op)
      {
        case ' ':
        case '\t':
        case '\r':
        case '\n':
        case ';':
          break;
        case 's': // regex
          try { opList.Operations.Add(new RegexReplaceActiveOp(await ReadRegexReplacementAsync(), traceInfo)); }
          catch (FormatException ex) { success = false; context.CompileOutputs.Add(new(CompileOutput.Levels.Error, ex.Message, traceInfo)); }
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
          catch (FormatException ex) { success = false; context.CompileOutputs.Add(new(CompileOutput.Levels.Error, ex.Message, traceInfo)); }
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
          catch (FormatException ex) { success = false; context.CompileOutputs.Add(new(CompileOutput.Levels.Error, ex.Message, traceInfo)); }
          break;
        case 'v': // regex with dynamic replacement
          try { opList.Operations.Add(new RegexReplaceActiveDynamicReplaceOp(await ReadRegexReplacementAsync(), traceInfo)); }
          catch (FormatException ex) { success = false; context.CompileOutputs.Add(new(CompileOutput.Levels.Error, ex.Message, traceInfo)); }
          break;
        case 'i': // input
          try { opList.Operations.Add(new InputOp(await ReadRegexReplacementAsync(), traceInfo)); }
          catch (FormatException ex) { success = false; context.CompileOutputs.Add(new(CompileOutput.Levels.Error, ex.Message, traceInfo)); }
          break;
        case 'o': // output
          try { opList.Operations.Add(new OutputOp(await ReadRegexReplacementAsync(), traceInfo)); }
          catch (FormatException ex) { success = false; context.CompileOutputs.Add(new(CompileOutput.Levels.Error, ex.Message, traceInfo)); }
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
            CompileContext loopContext = new(context) { EOB = EndOfBlock.Slash };
            LinearOperationList? loopOpList = await CompileOperationListAsync(loopContext, token);
            if (loopOpList == null)
              success = false;
            else
            {
              Regex? expression = null;
              try { expression = new(condition, options, matchTimeout: Regex.InfiniteMatchTimeout); }
              catch (ArgumentException)
              {
                context.CompileOutputs.Add(new(CompileOutput.Levels.Warning, WarnIllegalRegexRule, traceInfo));
              }
              opList.Operations.Add(new LoopOp(loopOpList, expression, traceInfo));
            }
          }
          catch (FormatException ex) { success = false; context.CompileOutputs.Add(new(CompileOutput.Levels.Error, ex.Message, traceInfo)); }
          break;
        case '\\': // LinearOperationList
          CompileContext blockContext = new(context) { EOB = EndOfBlock.Slash };
          var list = await CompileOperationListAsync(blockContext, token);
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
            CompileContext tryContext = new(context) { EOB = EndOfBlock.Slash };
            LinearOperationList? tryBlock = await CompileOperationListAsync(tryContext, token);
            string catchAs = await RegexReplacement.ReadStringAsync(reader, delimiter, token);
            CompileContext catchContext = new(context) { EOB = EndOfBlock.Slash };
            LinearOperationList? catchBlock = await CompileOperationListAsync(catchContext, token);
            if (tryBlock == null || catchBlock == null)
              success = false;
            else
            {
              Regex? expression = null;
              try { expression = new(condition, options, matchTimeout: Regex.InfiniteMatchTimeout); }
              catch (ArgumentException)
              {
                context.CompileOutputs.Add(new(CompileOutput.Levels.Warning, WarnIllegalRegexRule, traceInfo));
              }
              opList.Operations.Add(new CatchOp(tryBlock, catchBlock, expression, catchAs, traceInfo));
            }
          }
          catch (FormatException ex) { success = false; context.CompileOutputs.Add(new(CompileOutput.Levels.Error, ex.Message, traceInfo)); }
          break;
        case 'f': // function
          try
          {
            if (await reader.ReadAsync(OpCharBuffer, token) < 1)
              throw new FormatException("Missing delimiter.");
            char delimiter = OpCharBuffer[0];
            string op = await RegexReplacement.ReadStringAsync(reader, delimiter, token), name;
            switch (op)
            {
              case "s": // declare
                name = await RegexReplacement.ReadStringAsync(reader, delimiter, token);
                CompileContext declareContext = new(context) { EOB = EndOfBlock.Slash };
                LinearOperationList? loopOpList = await CompileOperationListAsync(declareContext, token);
                if (loopOpList == null)
                  success = false;
                else
                {
                  opList.Operations.Add(new DeclareFunctionOp(loopOpList, name, traceInfo));
                }
                break;
              case "u": // remove
                name = await RegexReplacement.ReadStringAsync(reader, delimiter, token);
                opList.Operations.Add(new DeclareFunctionOp(null, name, traceInfo));
                break;
              case "c": // call
                try { opList.Operations.Add(new CallFunctionOp(await ReadRegexReplacementAsync(delimiter), traceInfo)); }
                catch (FormatException ex) { success = false; context.CompileOutputs.Add(new(CompileOutput.Levels.Error, ex.Message, traceInfo)); }
                break;
              default:
                success = false; context.CompileOutputs.Add(new(CompileOutput.Levels.Error, $"Illegal function operation: {op}", traceInfo));
                break;
            }
          }
          catch (FormatException ex) { success = false; context.CompileOutputs.Add(new(CompileOutput.Levels.Error, ex.Message, traceInfo)); }
          break;
        case '/': // comment or end of block
          if (await reader.ReadAsync(OpCharBuffer, token) < 1) OpCharBuffer[0] = '\0';
          switch (OpCharBuffer[0])
          {
            case '/':
              await reader.ReadLineAsync(token);
              break;
            case '*':
              try { if (!(await reader.ReadUntilStringAsync("*/", token)).AsSpan()[^2..].SequenceEqual("*/")) throw new Exception(); }
              catch { context.CompileOutputs.Add(new(CompileOutput.Levels.Warning, "Comment not closed", GetTraceInfo())); }
              break;
            default:
              if (context.EOB == EndOfBlock.Slash)
              {
                ended = true;
                break;
              }
              context.CompileOutputs.Add(new(CompileOutput.Levels.Warning, "Use #, // or /* */, or use \\ if you are starting a block", GetTraceInfo()));
              await reader.ReadLineAsync(token);
              break;
          }
          break;
        default:
          success = false;
          context.CompileOutputs.Add(new(CompileOutput.Levels.Error, $"Illegal operation: {_op}", GetTraceInfo()));
          break;
      }
      if (context.EOB == EndOfBlock.EndOfOperation) break;
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
    Slash,
    /// <summary>
    /// Only read 1 operation then end
    /// </summary>
    EndOfOperation
  };
}