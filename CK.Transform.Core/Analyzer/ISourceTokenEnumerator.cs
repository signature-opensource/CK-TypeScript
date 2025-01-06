namespace CK.Transform.Core;

public interface ISourceTokenEnumerator
{
    public int Index { get; }

    public Token Token { get; }

    public SourceSpan Span { get; }

    public bool MoveNext();
}


