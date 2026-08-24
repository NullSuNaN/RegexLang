namespace RegexLang.Helper;

public sealed class PositionTrackingTextReader : TextReader
{
  private readonly TextReader _baseReader;
  private bool _isDisposed;
  private bool _lastCharWasCR;

  public int Line { get; private set; } = 1;
  public int Column { get; private set; } = 1;

  public PositionTrackingTextReader(TextReader baseReader)
  {
    _baseReader = baseReader ?? throw new ArgumentNullException(nameof(baseReader));
  }

  public override int Peek() => _baseReader.Peek();

  public override int Read()
  {
    int readChar = _baseReader.Read();
    if (readChar != -1)
    {
      AdvancePosition((char)readChar);
    }
    return readChar;
  }

  public override async Task<int> ReadAsync(char[] buffer, int index, int count)
  {
    int charsRead = await _baseReader.ReadAsync(buffer, index, count).ConfigureAwait(false);
    for (int i = 0; i < charsRead; i++)
    {
      AdvancePosition(buffer[index + i]);
    }
    return charsRead;
  }

#if NET6_0_OR_GREATER
  public override async ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default)
  {
    int charsRead = await _baseReader.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
    var span = buffer.Span;
    for (int i = 0; i < charsRead; i++)
    {
      AdvancePosition(span[i]);
    }
    return charsRead;
  }
#endif

  private void AdvancePosition(char c)
  {
    if (c == '\r')
    {
      Line++;
      Column = 1;
      _lastCharWasCR = true;
    }
    else if (c == '\n')
    {
      // If preceded by '\r', this '\n' is part of a \r\n pair, so line was already incremented
      if (!_lastCharWasCR)
      {
        Line++;
        Column = 1;
      }
      _lastCharWasCR = false;
    }
    else
    {
      Column++;
      _lastCharWasCR = false;
    }
  }

  protected override void Dispose(bool disposing)
  {
    if (!_isDisposed)
    {
      if (disposing)
      {
        _baseReader.Dispose();
      }
      _isDisposed = true;
    }
    base.Dispose(disposing);
  }
}