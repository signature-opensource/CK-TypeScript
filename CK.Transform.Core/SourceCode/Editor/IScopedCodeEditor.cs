namespace CK.Transform.Core;

public interface IScopedCodeEditor : ICodeEditor
{
    IFilteredTokenSpanEnumerator Tokens { get; }
}
