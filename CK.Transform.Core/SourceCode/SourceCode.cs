using CK.Core;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace CK.Transform.Core;

/// <summary>
/// Source code is created by a <see cref="TokenizerHead"/> and can be mutated by a <see cref="SourceCodeEditor"/>
/// (only from a <see cref="TransformerHost.Transform(IActivityMonitor, string, IEnumerable{CK.Transform.Core.TransformerFunction})"/>).
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class SourceCode
{
    internal readonly SourceSpanRoot _spans;
    // This can be a ImmutableList<Token> or a ImmutableList<Token>.Builder (a RB tree)
    // but ImmutableList is not that good for iteration and indexed access.
    // This should be benchmarked with real sized lists before considering this, but this is doable.
    List<Token> _tokens;
    string? _toString;

    internal SourceCode( List<Token> tokens, SourceSpanRoot spans, string? sourceText )
    {
        _spans = new SourceSpanRoot();
        if( spans._children._firstChild != null ) spans.TransferTo( _spans );
        _tokens = tokens;
        _toString = sourceText;
    }

    /// <summary>
    /// Gets the spans.
    /// </summary>
    public ISourceSpanRoot Spans => _spans;

    /// <summary>
    /// Gets the tokens.
    /// </summary>
    public IReadOnlyList<Token> Tokens => _tokens;

    internal List<Token> InternalTokens => _tokens;

    internal void OnTokensChanged()
    {
        _toString = null;
    }

    internal void TransferTo( SourceCode code )
    {
        code._tokens = _tokens;
        code._toString = _toString;
        code._spans._children.Clear();
        if( _spans._children.HasChildren ) _spans.TransferTo( code._spans );
    }

    /// <summary>
    /// Overridden to return the text of the source.
    /// </summary>
    /// <returns>The text.</returns>
    public override string ToString() => _toString ??= _tokens.Write( new StringBuilder() ).ToString();
}
