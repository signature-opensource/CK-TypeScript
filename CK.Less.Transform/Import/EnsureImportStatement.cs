using CK.Core;
using CK.Transform.Core;
using System;
using System.Linq;
using System.Text;

namespace CK.Less.Transform;

/// <summary>
/// Models the <c>@import ([!]<see cref="ImportKeyword"/>) '<see cref="ImportPath"/>';</c>.
/// This is an extended <see cref="ImportStatement"/> that supports excluding a keyword.
/// </summary>
public sealed class EnsureImportStatement : TransformStatement
{
    ImportKeyword _include;
    ImportKeyword _exclude;
    string _importPath;

    internal EnsureImportStatement( int beg, int end, ImportKeyword include, ImportKeyword exclude, string importPath )
        : base( beg, end )
    {
        _include = include;
        _exclude = exclude;
        _importPath = importPath;
    }

    /// <summary>
    /// Gets the keywords that must be included.
    /// </summary>
    public ImportKeyword Include => _include;

    /// <summary>
    /// Gets the keywords that must be excluded.
    /// This applies when an import with the <see cref="ImportPath"/> already exists.
    /// Exclusions take precedence over inclusion.
    /// </summary>
    public ImportKeyword Exclude => _exclude;

    /// <summary>
    /// Gets the import path.
    /// </summary>
    public string ImportPath => _importPath;

    /// <inheritdoc/>
    public override void Apply( IActivityMonitor monitor, SourceCodeEditor editor )
    {
        // No need to respect any scope here. @imports can appear anywhere in a less
        // file (note that it's not the case for css).

        // If we find an existing import, simply updates its keywords
        // and we are done.
        // Otherwise, we have to create a new @import.
        ImportStatement? lastImport = null;
        foreach( var import in editor.Code.Spans.OfType<ImportStatement>() )
        {
            if( import.ImportPath == _importPath )
            {
                import.SetImportKeyword( editor, _include, _exclude );
                return;
            }
            lastImport = import;
        }
        // We create a token with the whole text (without the comments as they belong to the transform langage)
        // and inserts in the the source code (after the last import or at the beginning of the source).
        var importLine = ImportStatement.Write( new StringBuilder(), _include, ImportKeyword.None, _importPath ).ToString();
        Token newText = new Token( TokenType.GenericAny, importLine, Trivia.NewLine );
        int insertionPoint = lastImport?.Span.End ?? 0;
        editor.InsertBefore( insertionPoint, newText );
        // We then create a brand new (1 token length) ImportStatement with the toMerge line
        // and we add it to the spans.
        var newStatement = new ImportStatement( insertionPoint, insertionPoint + 1, _include, _importPath );
        editor.AddSourceSpan( newStatement );
    }

    internal static EnsureImportStatement? TryMatch( int begEnsure, ref TokenizerHead head )
    {
        Throw.DebugAssert( head.LastToken != null && head.LastToken.Text.Span.Equals( "@import", StringComparison.Ordinal ) );

        // We consider the @import (!keyword) exclude.
        ImportKeyword include = ImportStatement.ParseKeywords( ref head, out ImportKeyword exclude );
        var p = head.MatchToken( TokenType.GenericString, "import path" );
        if( p is TokenError ) return null;
        var importPath = p.Text.Slice( 1, p.Text.Length - 2 ).ToString();
        head.TryAcceptToken( ";", out _ );
        return head.AddSpan( new EnsureImportStatement( begEnsure, head.LastTokenIndex + 1, include, exclude, importPath ) );
    }

    /// <summary>
    /// Overridden to return the statement.
    /// </summary>
    /// <returns>The statement.</returns>
    public override string ToString()
    {
        return ImportStatement.Write( new StringBuilder( "ensure " ), _include, _exclude, _importPath ).ToString();
    }
}
