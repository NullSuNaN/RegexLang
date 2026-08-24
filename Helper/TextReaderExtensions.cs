using System.Text;

namespace RegexLang.Helper;

public static class TextReaderExtensions
{
  public static async Task<string> ReadUntilStringAsync(this TextReader reader, string delimiter, CancellationToken token)
  {
    if (string.IsNullOrEmpty(delimiter))
      return await reader.ReadToEndAsync(token);

    var sb = new StringBuilder();
    char[] singleCharBuffer = new char[1];
    int matchIndex = 0;

    // Read exactly one character at a time asynchronously
    while (await reader.ReadAsync(singleCharBuffer, token) > 0)
    {
      char c = singleCharBuffer[0];
      sb.Append(c);

      if (c == delimiter[matchIndex])
      {
        matchIndex++;
        if (matchIndex == delimiter.Length)
        {
          // Delimiter fully matched
          return sb.ToString();
        }
      }
      else
      {
        // Reset state machine on mismatch
        matchIndex = (c == delimiter[0]) ? 1 : 0;
      }
    }

    return sb.ToString();
  }

}