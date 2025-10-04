using System.Collections;

namespace KeSpider;

public ref struct SpanSplitEnumerator(
    ReadOnlySpan<char> span,
    char separator = ' ',
    StringSplitOptions options = StringSplitOptions.None) : IEnumerator<ReadOnlySpan<char>>
{
    private readonly ReadOnlySpan<char> original = span;
    private readonly bool removeEmptyEntries = options.HasFlag(StringSplitOptions.RemoveEmptyEntries);
    private readonly bool trimEntries = options.HasFlag(StringSplitOptions.TrimEntries);
    private readonly char separator = separator;
    private ReadOnlySpan<char> span = span;
    private bool finished = false;

    readonly object IEnumerator.Current => throw new NotSupportedException();
    public ReadOnlySpan<char> Current { readonly get; private set; }
    public readonly SpanSplitEnumerator GetEnumerator() => this;

    public bool MoveNext()
    {
        while (true)
        {
            if (finished)
                return false;
            int separatorIndex = span.IndexOf(separator);
            ReadOnlySpan<char> tmp;
            if (separatorIndex == -1)
            {
                finished = true;
                tmp = span;
                if (trimEntries)
                    tmp = tmp.Trim();
                if (removeEmptyEntries && tmp.IsEmpty)
                    return false;
                Current = tmp;
                return true;
            }
            tmp = span[..separatorIndex];
            span = span[(separatorIndex + 1)..];
            if (trimEntries)
                tmp = tmp.Trim();
            if (removeEmptyEntries && tmp.IsEmpty)
                continue;
            Current = tmp;
            return true;
        }
    }

    public void Reset()
    {
        span = original;
        finished = false;
        Current = [];
    }

    public readonly void Dispose()
    {
    }
}

public static class SpanSplitExtension
{
    public static SpanSplitEnumerator Split(
        this ReadOnlySpan<char> span,
        char separator = ' ',
        StringSplitOptions options = StringSplitOptions.None)
    {
        return new(span, separator, options);
    }
}