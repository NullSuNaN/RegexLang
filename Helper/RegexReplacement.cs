using System.Text;
using System.Text.RegularExpressions;
using RegexLang.Runtime;

namespace RegexLang.Helper;

/// <summary>
/// A Regex replacing command
/// </summary>
/// <param name="Rule">the regular expression, null when parsing failed</param>
/// <param name="Replacement">replacing rule</param>
public partial record RegexReplacement(Regex? Rule, string Replacement, bool Global)
{
  /// <summary>
  /// Applies the substitution to the input string, must be executed synchronized with the context.
  /// </summary>
  public string? Apply(string input, TaskContext context)
  {
    if (Rule == null)
      return null;
    return Global
      ? Rule.Replace(input, Replacement)
      : Rule.Replace(input, Replacement, count: 1);
  }
  /// <summary>
  /// Applies the substitution to the input string, must be executed synchronized with the context.
  /// </summary>
  public string? ApplyWithDynamicReplacement(string input, TaskContext context)
  {
    string? dynamicReplacement;
    if (Rule == null || (dynamicReplacement = context.QueryEffectiveValue(Replacement)) == null)
      return null;
    dynamicReplacement = ConvertRegexReplacementSyntax(dynamicReplacement);
    return Global
      ? Rule.Replace(input, dynamicReplacement)
      : Rule.Replace(input, dynamicReplacement, count: 1);
  }

  /// <summary>
  /// Parses a sed replacement command (e.g., "/pattern/replacement/flags") asynchronously from a reader.
  /// Expects the stream pointer to be immediately after the leading op char.
  /// </summary>
  /// <param name="setDelimiter">Set delimiter, then do not read the leading one</param>
  /// <exception cref="ArgumentNullException" />
  /// <exception cref="FormatException" />
  public static async Task<RegexReplacement> ParseRegexStreamAsync(TextReader reader, char? setDelimiter = null, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(reader);

    // Read the delimiter character immediately following 's'
    Memory<char> delimiterBuffer = new char[1];
    if (setDelimiter == null)
    {
      int read = await reader.ReadAsync(delimiterBuffer, cancellationToken).ConfigureAwait(false);
      if (read == 0)
      {
        throw new FormatException("Missing delimiter.");
      }
    }
    char delimiter = setDelimiter ?? delimiterBuffer.Span[0];

    // Extract components
    string pattern = await ReadSegmentAsync(reader, delimiter, cancellationToken).ConfigureAwait(false);
    string replacement = await ReadSegmentAsync(reader, delimiter, cancellationToken).ConfigureAwait(false);
    string flags = await ReadFlagsAsync(reader, cancellationToken).ConfigureAwait(false);


    // Convert sed backreferences (\1, \2) to C# style ($1, $2)
    string csReplacement = ConvertRegexReplacementSyntax(replacement);

    var (options, global) = GetOptions(flags);

    Regex? rule = null;
    try
    {
      rule = new(pattern, options, matchTimeout: Regex.InfiniteMatchTimeout);
    }
    catch (ArgumentException) { }
    return new RegexReplacement(rule, csReplacement, global);
  }
  public static (RegexOptions Options, bool Global) GetOptions(string flags)
  {

    // Convert sed flags to C# RegexOptions
    RegexOptions options = RegexOptions.None;
    bool global = false;

    foreach (char flag in flags)
    {
      switch (flag)
      {
        case 'g':
          global = true;
          break;
        case 'i':
          options |= RegexOptions.IgnoreCase;
          break;
        case 'm':
          options |= RegexOptions.Multiline;
          break;
        case 's':
          options |= RegexOptions.Singleline;
          break;
        default:
          throw new FormatException($"Unsupported sed flag '{flag}'.");
      }
    }
    return (options, global);
  }
  public static Task<string> ReadStringAsync(TextReader reader, char delimiter, CancellationToken cancellationToken) =>
    ReadSegmentAsync(reader, delimiter, cancellationToken);

  private static async Task<string> ReadSegmentAsync(TextReader reader, char delimiter, CancellationToken cancellationToken)
  {
    var sb = new StringBuilder();
    bool isEscaped = false;
    Memory<char> buffer = new char[1];

    while (await reader.ReadAsync(buffer, cancellationToken).ConfigureAwait(false) > 0)
    {
      char c = buffer.Span[0];

      if (isEscaped)
      {
        // If escaping the delimiter, write the delimiter itself
        if (c == delimiter)
          sb.Append(c);
        else
          sb.Append('\\').Append(c);
        isEscaped = false;
      }
      else if (c == '\\')
      {
        isEscaped = true;
      }
      else if (c == delimiter)
      {
        return sb.ToString();
      }
      else
      {
        sb.Append(c);
      }
    }

    throw new FormatException($"Unterminated segment. Missing closing delimiter '{delimiter}'.");
  }

  private static async Task<string> ReadFlagsAsync(TextReader reader, CancellationToken cancellationToken)
  {
    var sb = new StringBuilder();

    // Since TextReader does not have PeekAsync(), we read one character at a time.
    // If it's a flag (letter), we append it. If not, we cannot "unread" it directly,
    // but sed flags are strictly trailing characters at the end of the expression.
    Memory<char> buffer = new char[1];

    while (true)
    {
      // Note: If you need to preserve trailing non-flag characters in the stream, 
      // you can inspect underlying buffers or wrap the reader in a custom peeking stream.
      int count = await reader.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
      if (count == 0) break;

      char c = buffer.Span[0];
      if (char.IsLetter(c))
      {
        sb.Append(c);
      }
      else
      {
        break;
      }
    }

    return sb.ToString();
  }

  [GeneratedRegex(@"\\\\|\\([0-9])|\\(.)|&|\$")]
  private static partial Regex RegexTokenRegex();

  private static string ConvertRegexReplacementSyntax(string replacement)
  {
    return RegexTokenRegex().Replace(replacement, match =>
    {
      string val = match.Value;
      // 1. Whole match shorthand: & -> $0
      if (val == "&")
      {
        return "${0}";
      }

      // 2. Existing '$' -> escape to '$$' so .NET treats it as literal '$'
      if (val == "$")
      {
        return "$$";
      }

      // 3. Backslash sequence: \0, \1, \n, etc.
      if (val.StartsWith('\\') && val.Length > 1)
      {
        char groupOrEscaped = val[1];

        // \0 -> whole match ($0)
        if (val == @"\0")
        {
          return "${0}";
        }

        // \1, \2, etc. -> group references ($1, $2)
        if (char.IsDigit(groupOrEscaped))
        {
          string groupNum = val[1..];
          return $"${{{groupNum}}}";
        }

        // Any other escaped character (e.g., \/, \@) -> pass as literal
        return groupOrEscaped.ToString();
      }

      return val;
    });
  }
}