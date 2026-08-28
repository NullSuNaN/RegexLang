using System.Text;
using AsyncConsoleReader;

namespace RegexLang.Program.Shell;

public class ConsoleInputReader : TextReader
{
    private readonly CancellationToken _cancellationToken;
    private readonly string _newLine;
    private string? _currentBuffer;
    private int _bufferIndex;
    private bool _isEndOfStream;

    /// <summary>
    /// Initializes a new instance of <see cref="ConsoleInputReader"/> backed by <see cref="AsyncConsole"/>.
    /// </summary>
    /// <param name="cancellationToken">Global cancellation token for console read operations.</param>
    /// <param name="newLine">Custom newline character sequence appended to buffered lines.</param>
    public ConsoleInputReader(CancellationToken cancellationToken = default, string? newLine = null)
    {
        _cancellationToken = cancellationToken;
        _newLine = newLine ?? Environment.NewLine;
    }

    /// <summary>
    /// Asynchronously reads characters into a character memory buffer using AsyncConsole.
    /// </summary>
    public override async ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default)
    {
        if (buffer.IsEmpty) return 0;

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken, cancellationToken);

        if (!await EnsureBufferAsync(linkedCts.Token).ConfigureAwait(false))
        {
            return 0; // EOF
        }

        int charsToRead = Math.Min(buffer.Length, _currentBuffer!.Length - _bufferIndex);
        _currentBuffer.AsSpan(_bufferIndex, charsToRead).CopyTo(buffer.Span);
        _bufferIndex += charsToRead;

        return charsToRead;
    }

    /// <summary>
    /// Asynchronously reads characters into an array buffer.
    /// </summary>
    public override async Task<int> ReadAsync(char[] buffer, int index, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (buffer.Length - index < count)
        {
            throw new ArgumentException("Invalid offset and length array bounds.");
        }

        if (count == 0) return 0;

        if (!await EnsureBufferAsync(_cancellationToken).ConfigureAwait(false))
        {
            return 0; // EOF
        }

        int charsToRead = Math.Min(count, _currentBuffer!.Length - _bufferIndex);
        _currentBuffer.CopyTo(_bufferIndex, buffer, index, charsToRead);
        _bufferIndex += charsToRead;

        return charsToRead;
    }

    /// <summary>
    /// Reads a line asynchronously using AsyncConsole.ReadLineAsync.
    /// </summary>
    public override async Task<string?> ReadLineAsync()
    {
        // If we have remaining buffered characters, read up to the next newline
        if (_currentBuffer != null && _bufferIndex < _currentBuffer.Length)
        {
            var sb = new StringBuilder();
            while (await EnsureBufferAsync(_cancellationToken).ConfigureAwait(false))
            {
                char c = _currentBuffer![_bufferIndex++];
                if (c == '\n')
                {
                    break;
                }
                if (c == '\r')
                {
                    if (Peek() == '\n')
                    {
                        _bufferIndex++;
                    }
                    break;
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        // Direct line fetch if buffer is empty
        _currentBuffer = null;
        _bufferIndex = 0;

        return await FetchLineAsync(_cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Synchronously reads the next character. 
    /// Converts the underlying ValueTask to a Task to prevent undefined state access.
    /// </summary>
    public override int Read()
    {
        Memory<char> singleCharBuffer = new char[1];
        int count = ReadAsync(singleCharBuffer, _cancellationToken).AsTask().GetAwaiter().GetResult();
        
        return count > 0 ? singleCharBuffer.Span[0] : -1;
    }

    /// <summary>
    /// Synchronously reads a line of text.
    /// </summary>
    public override string? ReadLine()
    {
        return ReadLineAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Ensures that _currentBuffer contains unread data by reading from AsyncConsole.
    /// </summary>
    private async Task<bool> EnsureBufferAsync(CancellationToken cancellationToken)
    {
        if (_isEndOfStream) return false;

        if (_currentBuffer != null && _bufferIndex < _currentBuffer.Length)
        {
            return true;
        }

        string? line = await FetchLineAsync(cancellationToken).ConfigureAwait(false);

        if (line == null)
        {
            _isEndOfStream = true;
            return false;
        }

        _currentBuffer = line + _newLine;
        _bufferIndex = 0;
        return true;
    }

    private async Task<string?> FetchLineAsync(CancellationToken cancellationToken)
    {
        while (!_isEndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Reads line asynchronously via AsyncConsoleReader package
                return await AsyncConsole.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (IOException)
            {
                // Retry continuous read on transient I/O interruptions
                continue;
            }
        }

        return null;
    }
}