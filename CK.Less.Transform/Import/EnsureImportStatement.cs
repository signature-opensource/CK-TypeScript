using CK.Core;
using CK.Transform.Core;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        var updated = FindAndUpdate( editor, _include, _exclude, _importPath, out var lastImport );
        if( updated == null )
        {
            Create( editor, _include, _importPath, lastImport );
        }
    }

    static ImportStatement? FindAndUpdate( SourceCodeEditor editor,
                                           ImportKeyword include,
                                           ImportKeyword exclude,
                                           string importPath,
                                           out ImportStatement? lastImport )
    {
        lastImport = null;
        foreach( var import in editor.Code.Spans.OfType<ImportStatement>() )
        {
            lastImport = import;
            if( import.ImportPath == importPath )
            {
                import.SetImportKeyword( editor, include, exclude );
                return import;
            }
        }
        return null;
    }

    static ImportStatement Create( SourceCodeEditor editor,
                                   ImportKeyword include,
                                   string importPath,
                                   ImportStatement? previousStatement )
    {
        int insertionPoint = previousStatement?.Span.End ?? 0;
        // We create a token with the whole text (without the comments as they belong to the transform langage)
        // and inserts in the the source code (after the last import or at the beginning of the source).
        var importLine = ImportStatement.Write( new StringBuilder(), include, ImportKeyword.None, importPath ).ToString();
        Token newText = new Token( TokenType.GenericAny, importLine, Trivia.NewLine );
        using( var e = editor.OpenGlobalEditor() )
        {
            e.InsertBefore( insertionPoint, newText );
            // We then create a brand new (1 token length) ImportStatement with the toMerge line
            // and we add it to the spans.
            var newStatement = new ImportStatement( insertionPoint, insertionPoint + 1, include, importPath );
            editor.AddSourceSpan( newStatement );
            return newStatement;
        }
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

    /// <summary>
    /// Ensures that the <paramref name="imports"/> appear in the enumerated order.
    /// </summary>
    /// <param name="editor">The code editor.</param>
    /// <param name="imports">The ordered imports.</param>
    public static void EnsureOrderedImports( SourceCodeEditor editor,
                                             IEnumerable<EnsureImportLine> imports )
    {
        ImportStatement? lastOrdered = null;
        foreach( var i in imports )
        {
            var found = FindAndUpdate( editor, i.Include, i.Exclude, i.ImportPath, out var veryLastImport );
            if( found == null )
            {
                lastOrdered = Create( editor, i.Include, i.ImportPath, lastOrdered );
            }
            else
            {
                // The @import has been found but is it well positioned?
                // If we have no lastOrdered yet then it is our first import, we have nothing to do.
                if( lastOrdered != null && lastOrdered.Span.End > found.Span.End )
                {
                    // found should be before lastOrdered!
                    editor.MoveSpanBefore( found, lastOrdered );
                }
            }

        }
    }
}
