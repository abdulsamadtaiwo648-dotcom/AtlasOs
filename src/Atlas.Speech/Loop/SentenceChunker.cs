using System.Text;

namespace Atlas.Speech.Loop;

/// <summary>
/// Turns a stream of text fragments (as an AI model produces them) into whole
/// sentences, so speech can start as soon as the first sentence is complete.
/// </summary>
public sealed class SentenceChunker
{
    private readonly StringBuilder _buffer = new();
    private readonly int _minLength;
    private readonly int _maxLength;

    public SentenceChunker(int minLength = 20, int maxLength = 250)
    {
        _minLength = minLength;
        _maxLength = maxLength;
    }

    /// <summary>Adds text and returns any sentences that are now complete.</summary>
    public IEnumerable<string> Push(string fragment)
    {
        List<string> ready = new();

        foreach (char c in fragment)
        {
            _buffer.Append(c);

            if (IsBoundary(c))
            {
                Emit(ready);
            }
            else if (_buffer.Length >= _maxLength && c == ' ')
            {
                Emit(ready, force: true);
            }
        }

        return ready;
    }

    /// <summary>Returns whatever text is left at the end of the reply.</summary>
    public IEnumerable<string> Flush()
    {
        string rest = _buffer.ToString().Trim();
        _buffer.Clear();

        if (rest.Length > 0)
            yield return rest;
    }

    // A sentence ends at . ! ? only when followed by whitespace (so "3.5" and
    // "e.g." stay intact) - we can't see the next char yet, so the check is
    // made when the whitespace arrives. Newlines also end a sentence.
    private bool IsBoundary(char c)
    {
        if (c == '\n')
            return _buffer.Length >= 2;

        if (!char.IsWhiteSpace(c) || _buffer.Length < 2)
            return false;

        char previous = _buffer[^2];

        return previous is '.' or '!' or '?' && _buffer.Length >= _minLength;
    }

    private void Emit(List<string> into, bool force = false)
    {
        string sentence = _buffer.ToString().Trim();
        _buffer.Clear();

        if (sentence.Length > 0 || force)
            into.Add(sentence);
    }
}
