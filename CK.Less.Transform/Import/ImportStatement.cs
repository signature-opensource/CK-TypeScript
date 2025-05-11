using CK.Core;
using CK.Transform.Core;
using System;
using System.Text;

namespace CK.Less.Transform;

/// <summary>
/// Models the <c>@import (<see cref="ImportKeyword"/>) '<see cref="ImportPath"/>';</c>.
/// </summary>
public sealed class ImportStatement : SourceSpan
{
    ImportKeyword _keyword;
    string _importPath;

    internal ImportStatement( int beg, int end, ImportKeyword keyword, string importPath )
        : base( beg, end )
    {
        _keyword = keyword;
        _importPath = importPath;
    }

    /// <summary>
    /// Gets the import keywords.
    /// </summary>
    public ImportKeyword ImportKeyword => _keyword;

    /// <summary>
    /// Gets the import path.
    /// </summary>
    public string ImportPath => _importPath;

    static ImportKeyword Normalize( ImportKeyword newOne )
    {
        // Multiple has the priority over the 'once' default.
        if( (newOne & ImportKeyword.Multiple) != 0 ) newOne &= ~ImportKeyword.Once;
        if( (newOne & ImportKeyword.Once) != 0 ) newOne &= ~ImportKeyword.Multiple;

        // Less has the priority over 'css'.
        if( (newOne & ImportKeyword.Less) != 0 ) newOne &= ~ImportKeyword.Css;
        if( (newOne & ImportKeyword.Css) != 0 ) newOne &= ~ImportKeyword.Less;

        return newOne;
    }

    #region Mutators
    // Quick implementation: mutation becomes a mono-token (potential comments inside are lost... but who cares?).

    internal void SetImportKeyword( SourceCodeEditor editor, ImportKeyword include, ImportKeyword exclude )
    {
        // Accounting the priorities.
        if( (include & ImportKeyword.Css) != 0 ) exclude |= ImportKeyword.Less;
        if( (include & ImportKeyword.Once) != 0 ) exclude |= ImportKeyword.Multiple;
        var newOne = (_keyword & ~exclude) | include;
        newOne = Normalize( newOne );
        if( newOne != _keyword )
        {
            _keyword = newOne;
            SetMonoToken( editor );
        }
    }

    internal void SetImportPath( SourceCodeEditor editor, string importPath )
    {
        if( _importPath != importPath )
        {
            _importPath = importPath;
            SetMonoToken( editor );
        }
    }

    void SetMonoToken( SourceCodeEditor editor )
    {
        var startToken = editor.Code.Tokens[Span.Beg];
        var endToken = editor.Code.Tokens[Span.End - 1];
        Token newToken = new Token( startToken.TokenType, startToken.LeadingTrivias, ToString(), endToken.TrailingTrivias );
        using var e = editor.OpenEditor();
        e.Replace( Span.Beg, Span.Length, newToken );
    }
    #endregion // Mutators

    internal static ImportStatement? TryMatch( Token importToken, ref TokenizerHead head )
    {
        Throw.DebugAssert( importToken.Text.Span.Equals( "@import", StringComparison.Ordinal ) );
        int begImport = head.LastTokenIndex;

        // We ignore here any @import (!keyword) as this is for transform statement only.
        ImportKeyword keyword = ParseKeywords( ref head, out _ );
        var p = head.MatchToken( TokenType.GenericString, "import path" );
        if( p is TokenError ) return null;
        var importPath = p.Text.Slice( 1, p.Text.Length - 2 ).ToString();
        head.TryAcceptToken( ";", out _ );
        return head.AddSpan( new ImportStatement( begImport, head.LastTokenIndex + 1, keyword, importPath ) );
    }

    // This can emit an unexpected error in the head.
    // This is alos used by the EnsureImportStatement (hence the handling of the excludedKeyword).
    internal static ImportKeyword ParseKeywords( ref TokenizerHead head, out ImportKeyword excludedKeyword )
    {
        excludedKeyword = ImportKeyword.None;
        ImportKeyword keyword = ImportKeyword.None;
        if( head.TryAcceptToken( TokenType.OpenParen, out _ ) )
        {
            // Allow "@import () 'file'" (even if this is not allowed).
            while( !head.TryAcceptToken( TokenType.CloseParen, out _ ) )
            {
                // Transform extension.
                if( head.TryAcceptToken( TokenType.Exclamation, out _ ) )
                {
                    HandleKeyword( ref head, ref excludedKeyword );
                }
                else
                {
                    HandleKeyword( ref head, ref keyword );
                }
                // Allow traing comma (even if this is not allowed).
                head.TryAcceptToken( TokenType.Comma, out _ );
            }
            excludedKeyword = Normalize( excludedKeyword );
            keyword = Normalize( keyword & ~excludedKeyword );
        }
        return keyword;

        static void HandleKeyword( ref TokenizerHead head, ref ImportKeyword keyword )
        {
            if( head.TryAcceptToken( "once", out _ ) )
            {
                keyword |= ImportKeyword.Once;
            }
            else if( head.TryAcceptToken( "multiple", out _ ) )
            {
                keyword |= ImportKeyword.Multiple;
            }
            else if( head.TryAcceptToken( "reference", out _ ) )
            {
                keyword |= ImportKeyword.Reference;
            }
            else if( head.TryAcceptToken( "less", out _ ) )
            {
                keyword |= ImportKeyword.Less;
            }
            else if( head.TryAcceptToken( "css", out _ ) )
            {
                keyword |= ImportKeyword.Css;
            }
            else if( head.TryAcceptToken( "optional", out _ ) )
            {
                keyword |= ImportKeyword.Optional;
            }
            else if( head.TryAcceptToken( "inline", out _ ) )
            {
                keyword |= ImportKeyword.Inline;
            }
            else
            {
                head.AppendUnexpectedToken();
            }
        }
    }

    /// <summary>
    /// Overridden to return the @import statement.
    /// </summary>
    /// <returns>The @import statement.</returns>
    public override string ToString() => Write( new StringBuilder(), _keyword, ImportKeyword.None, _importPath ).ToString();

    // This never writes 'once' as it is the default.
    internal static StringBuilder Write( StringBuilder b, ImportKeyword include, ImportKeyword exclude, string importPath )
    {
        var hasKeyWords = include is not ImportKeyword.None and not ImportKeyword.Once
                          || exclude is not ImportKeyword.None and not ImportKeyword.Once;

        b.Append( "@import " );
        if( hasKeyWords )
        {
            b.Append( '(' );
            bool atLeastOne = false;
            WriteKeywords( false, include, b, ref atLeastOne );
            if( exclude is not ImportKeyword.None and not ImportKeyword.Once )
            {
                WriteKeywords( true, exclude, b, ref atLeastOne );
            }
            Throw.DebugAssert( atLeastOne );
            b.Append( ") '" );
        }
        else
        {
            b.Append( '\'' );
        }
        b.Append( importPath ).Append( "';" );
        return b;

        static void WriteKeywords( bool exclude, ImportKeyword kw, StringBuilder b, ref bool atLeastOne )
        {
            if( (kw & ImportKeyword.Multiple) != 0 )
            {
                Throw.DebugAssert( (kw & ImportKeyword.Once) == 0 );
                Write( exclude, b, ref atLeastOne, "multiple" );
            }
            if( (kw & ImportKeyword.Reference) != 0 )
            {
                Write( exclude, b, ref atLeastOne, "reference" );
            }
            if( (kw & ImportKeyword.Inline) != 0 )
            {
                Write( exclude, b, ref atLeastOne, "inline" );
            }
            if( (kw & ImportKeyword.Optional) != 0 )
            {
                Write( exclude, b, ref atLeastOne, "optional" );
            }
            if( (kw & ImportKeyword.Less) != 0 )
            {
                Throw.DebugAssert( (kw & ImportKeyword.Css) == 0 );
                Write( exclude, b, ref atLeastOne, "less" );
            }
            if( (kw & ImportKeyword.Css) != 0 )
            {
                Throw.DebugAssert( (kw & ImportKeyword.Less) == 0 );
                Write( exclude, b, ref atLeastOne, "css" );
            }

            static void Write( bool exclude, StringBuilder b, ref bool atLeastOne, string keyword )
            {
                if( atLeastOne ) b.Append( ", " );
                if( exclude ) b.Append( '!' );
                b.Append( keyword );
                atLeastOne = true;
            }
        }
    }

}
