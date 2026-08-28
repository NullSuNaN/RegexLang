using RegexLang.Code;

namespace RegexLang.Runtime;

public record struct RegexException(string Name, string Description, FileTraceInfo? FileTrace)
{
  public const string NoActiveValueException = "null_active";
  public const string NoReplacementException = "no_repl";
  public const string NoFunctionNameException = "no_fn_name";
  public const string NoFunctionException = "no_fn";
  public const string ParsingException = "parse";
  public const string NumberParsingException = "parse_num";
  public const string OperationCancelledException = "cancel";
  public const string IOException = "io";
}