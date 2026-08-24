using RegexLang.Code;
using RegexLang.Helper;
using RegexLang.Operation;

namespace RegexLang.Runtime;

/// <summary>
/// A non-concurrent task context, only for one linear task, async tasks should spawn new ones
/// </summary>
public class TaskContext
{
  public string ActiveIndex = InitialActiveIndex;
  private readonly TrieStringDictionary<string> StoredValues = [new(InitialActiveIndex, InitialActiveValue)];
  public TrieStringDictionary<string> GetRawStoredValues_() => StoredValues;
  public readonly TrieStringDictionary<IOperation> StoredFunctions = [];
  public string? ActiveValue
  {
    get => QueryEffectiveValue(ActiveIndex);
    set => SetEffectiveValue(ActiveIndex, value);
  }
  public bool HaveException => StoredValues.ContainsKey(ExceptionTypeField);

  public const string InitialActiveIndex = "var";
  public static readonly string InitialActiveValue = string.Concat(Enumerable.Range(0, 256).Select(i => (char)i));
  public const string ExceptionTypeField = "#exc";
  public const string ExceptionDescriptionField = "#exc_desc";
  public const string ExceptionTraceField = "#exc_trace";
  public const string BinaryExitValueField = "#exit";
  public const string NoTimeoutValue = "inf";
  public void Throw(RegexException ex)
  {
    SetEffectiveValue(ExceptionTypeField, ex.Name);
    SetEffectiveValue(ExceptionDescriptionField, ex.Description);
    SetEffectiveValue(ExceptionTraceField, ex.FileTrace?.ToString());
  }

  public bool Catch()
  {
    if (StoredValues.ContainsKey(ExceptionTypeField))
      return true;
    else
      return false;
  }

  /// <summary>
  /// Query an effective value
  /// </summary>
  /// <param name="name">
  ///   The name, if it starts with @, then it will return the value of the field referenced by the value.
  ///   If multiple ones are given, it would read the literal value of the variable with one leading @ removed from the name.
  /// </param>
  /// <returns>the value, null if it does not exist</returns>
  public string? QueryEffectiveValue(string name)
  {
    if (name.StartsWith("@@"))
      return StoredValues.TryGetValue(name[1..], out var val) ? val : null;
    else if (name.StartsWith('@'))
    {
      StoredValues.TryGetValue(name[1..], out var refName);
      if (refName == null) return null;
      return StoredValues.TryGetValue(refName, out var val) ? val : null;
    }
    else
      return StoredValues.TryGetValue(name, out var val) ? val : null;
  }
  /// <summary>
  /// Query an effective variable name
  /// </summary>
  /// <param name="name">
  ///   The name, if it starts with @, then it will return the name.
  ///   If multiple ones are given, it would read the literal name with one leading @ removed from the name.
  /// </param>
  /// <returns>the value, null if it does not exist</returns>
  public string? QueryEffectiveVariable(string name)
  {
    if (name.StartsWith("@@"))
      return name[1..];
    else if (name.StartsWith('@'))
    {
      StoredValues.TryGetValue(name[1..], out var refName);
      return refName;
    }
    else
      return name;
  }
  /// <summary>
  /// Set an effective value
  /// </summary>
  /// <param name="name">
  ///   The name, if it starts with @, then it will return the value of the field referenced by the value.
  ///   If multiple ones are given, it would read the literal value of the variable with one leading @ removed from the name.
  /// </param>
  public void SetEffectiveValue(string name, string? value)
  {
    if (name.StartsWith("@@"))
      SetValue(name[1..], value);
    else if (name.StartsWith('@'))
    {
      StoredValues.TryGetValue(name[1..], out var refName);
      if (refName == null) return;
      SetValue(refName, value);
    }
    else
      SetValue(name, value);
  }
  private void SetValue(string name, string? value)
  {
    if(value == null) StoredValues.Remove(name);
    else StoredValues[name] = value;
  }

  public bool PrintException()
  {
    string? result = QueryEffectiveValue(ExceptionTypeField);
    if(result == null) return false;
    Console.WriteLine($"Exception {result} at {QueryEffectiveValue(ExceptionTraceField)}: {QueryEffectiveValue(ExceptionDescriptionField)}");
    return true;
  }

  /// <summary>
  /// Check if there is an exception, and complete the file trace field
  /// </summary>
  /// <param name="fileTrace">The file trace to use if it does not have one originally(write op thrown)</param>
  /// <returns>if there is an exception</returns>
  public bool CheckException(FileTraceInfo? fileTrace)
  {
    string? exception = QueryEffectiveValue(ExceptionTypeField);
    if(exception == null) return false;
    if(QueryEffectiveValue(ExceptionTraceField) == null) SetEffectiveValue(ExceptionTraceField, fileTrace?.ToString());
    return true;
  }
}