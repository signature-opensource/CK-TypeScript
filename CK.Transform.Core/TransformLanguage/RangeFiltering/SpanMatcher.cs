using CK.Core;

namespace CK.Transform.Core;

/// <summary>
/// Captures a span matcher:
/// <para>
/// {<see cref="SpanType"/>} .<see cref="LanguageHint"/><see cref="Pattern"/>
/// </para>
/// Where the span type is optional (defaults to the matched tokens) and the .<see cref="LanguageHint"/>
/// prefix is also optional (defaults to the target language).
/// </summary>
public sealed class SpanMatcher : SourceSpan
{
    string? _spanType;
    string? _languageHint;
    RawString _pattern;

    public SpanMatcher( int beg, int end, string? spanType, string? languageHint, RawString pattern )
        : base( beg, end )
    {
        _spanType = spanType;
        _languageHint = languageHint;
        _pattern = pattern;
    }

    /// <summary>
    /// Gets or sets the type of the span.
    /// </summary>
    public string? SpanType { get => _spanType; set => _spanType = value; }

    /// <summary>
    /// Gets or sets the optional language hint that must be one of <see cref="TransformLanguage.FileExtensions"/>.
    /// When null, the target laguage is used.
    /// </summary>
    public string? LanguageHint { get => _languageHint; set => _languageHint = value; }

    /// <summary>
    /// Gets or sets the pattern to match.
    /// The <see cref="RawString.InnerText"/> must not be empty.
    /// </summary>
    public RawString Pattern
    {
        get => _pattern;
        set
        {
            Throw.CheckArgument( value.InnerText.Length > 0 );
            _pattern = value;
        }
    }

    internal static SpanMatcher? Match( ref TokenizerHead head )
    {
        int begSpan = head.LastTokenIndex + 1;
        string? spanType = null;
        if( head.TryAcceptToken(TokenType.OpenBrace, out _ ) )
        {
            if( head.TryAcceptToken( TokenType.GenericIdentifier, out var sType ) )
            {
                spanType = sType.Text.ToString();
            }
            if( !head.TryAcceptToken( TokenType.CloseBrace, out _ ) )
            {
                head.AppendMissingToken( "closing '}'" );
            }
        }
        string? languageHint = null;
        if( head.TryAcceptToken(TokenType.Dot, out _))
        {
            if( head.TryAcceptToken( TokenType.GenericIdentifier, out var sType ) )
            {
                languageHint = sType.Text.ToString();
            }
            else
            {
                head.AppendMissingToken( "language name or file extension" );
            }
        }
        RawString? pattern = null;
        if( head.LowLevelTokenType is not TokenType.DoubleQuote )
        {
            head.AppendMissingToken( "Pattern string" );
        }
        else
        {
            pattern = RawString.TryMatch( ref head );
            if( pattern != null && pattern.InnerText.Length == 0 )
            {
                head.AppendError( "Pattern string must not be empty.", -1 );
                pattern = null;
            }
        }
        head.TryAcceptToken( TokenType.SemiColon, out _ );
        return pattern == null
                ? null
                : new SpanMatcher( begSpan, head.LastTokenIndex + 1, spanType, languageHint, pattern );
    }
}
