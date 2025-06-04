using CK.Core;
using CK.Transform.Core;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;

namespace CK.TypeScript.Transform;

/// <summary>
/// Mutable <see cref="IImportLine"/>.
/// </summary>
public sealed class ImportLine : IImportLine
{
    /// <summary>
    /// Initializes a new empty (<see cref="SideEffectOnly"/>) import line
    /// with an empty <see cref="ImportPath"/>.
    /// </summary>
    public ImportLine()
    {
        NamedImports = new List<NamedImport>();
        ImportPath = string.Empty;
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    /// <param name="l">Source line to clone.</param>
    public ImportLine( IImportLine l )
    {
        TypeOnly = l.TypeOnly;
        NamedImports = new List<NamedImport>( l.NamedImports );
        Namespace = l.Namespace;
        DefaultImport = l.DefaultImport;
        ImportPath = l.ImportPath;
    }

    /// <summary>
    /// Models a named import.
    /// </summary>
    /// <param name="ExportedName">Exported name.</param>
    /// <param name="ImportedName">Imported alias name.</param>
    /// <param name="TypeOnly">Whether this is a type only import.</param>
    public readonly record struct NamedImport( string ExportedName, string? ImportedName, bool TypeOnly )
    {
        /// <summary>
        /// Gets whether the <see cref="ExportedName"/> is aliased: <see cref="ImportedName"/> is not null.
        /// </summary>
        [MemberNotNullWhen( true, nameof( ImportedName ) )]
        public bool IsAliased => ImportedName != null;

        /// <summary>
        /// Gets whether this is the invalid <c>default</c>.
        /// </summary>
        public bool IsDefault => ExportedName == null;

        /// <summary>
        /// Gets the ExportedName if ImportedName is null.
        /// </summary>
        public string FinalName => ImportedName ?? ExportedName;

        /// <summary>
        /// Overridden to return "ExportedName as ImportedName" or "ExportedName".
        /// </summary>
        /// <returns>The import names.</returns>
        public override string ToString() => IsAliased ? $"{ExportedName} as {ImportedName}" : ExportedName;
    }

    /// <inheritdoc />
    public bool SideEffectOnly => Namespace == null && DefaultImport == null && NamedImports.Count == 0;

    /// <inheritdoc />
    public bool TypeOnly { get; set; }

    /// <inheritdoc />
    public string? Namespace { get; set; }

    /// <inheritdoc />
    public string? DefaultImport { get; set; }

    /// <inheritdoc />
    public List<NamedImport> NamedImports { get; }

    /// <summary>
    /// Gets or sets the import path. Defaults to empty.
    /// </summary>
    public string ImportPath { get; set; }

    /// <inheritdoc />
    public string ToStringImport() => ToString();

    IReadOnlyList<NamedImport> IImportLine.NamedImports => NamedImports;

    internal void RemoveTypeOnly( SourceCodeEditor editor, TokenSpan span )
    {
        Throw.DebugAssert( TypeOnly );
        TypeOnly = false;
        if( span.Length == 1 )
        {
            MonoTokenUpdate( editor, span );
        }
        else
        {
            using var e = editor.OpenGlobalEditor();
            e.RemoveAt( span.Beg + 1 );
        }
    }

    internal void SetNamedImportType( SourceCodeEditor editor, TokenSpan span, int index, bool set )
    {
        Throw.DebugAssert( index >= 0 && index < NamedImports.Count );
        var n = NamedImports[index];
        Throw.DebugAssert( n.TypeOnly != set );

        // Updates the NamedImport.Type flag.
        NamedImports[index] = new NamedImport( n.ExportedName, n.ImportedName, set );
        if( span.Length == 1 )
        {
            MonoTokenUpdate( editor, span );
        }
        else
        {
            int tokenIndex = GetTokenIndexOfNamedIndex( span, index );
            using var e = editor.OpenGlobalEditor();
            if( set )
            {
                Token newToken = new Token( TokenType.GenericIdentifier, "type", Trivia.OneSpace );
                e.InsertAt( tokenIndex, newToken );
            }
            else
            {
                e.RemoveAt( tokenIndex );
            }
        }
    }

    internal void AddNamedImport( SourceCodeEditor editor, TokenSpan span, NamedImport named )
    {
        if( span.Length == 1 )
        {
            NamedImports.Add( named );
            MonoTokenUpdate( editor, span );
        }
        else
        {
            // The name doesn't exist, GetTokenIndexOfNamedIndex returns the insertion
            // point (the token before which me must insert).
            // We start from the previous token.
            int tokenIndex = GetTokenIndexOfNamedIndex( span, NamedImports.Count ) - 1;
            bool isSideEffectOnly = SideEffectOnly;
            // If isSideEffectOnly we are on the "import": we must not add a comma.
            bool needCommaPrefix = !isSideEffectOnly;
            bool needBraces = NamedImports.Count == 0;

            // We can add the name now.
            NamedImports.Add( named );
            // We replace the previous (to be able to handle the transfer of the trivias for the comma).
            var b = new TokenListBuilder { editor.Code.Tokens[tokenIndex] };
            if( needBraces )
            {
                if( needCommaPrefix )
                {
                    b.RemoveSingleTrailingWhitespace();
                    b.Add( new Token( TokenType.Comma, ",", Trivia.OneSpace ) );
                }
                b.Add( new Token( TokenType.OpenBrace, "{", Trivia.OneSpace ) );
                needCommaPrefix = false;
            }
            if( needCommaPrefix )
            {
                b.RemoveSingleTrailingWhitespace();
                b.Add( new Token( TokenType.Comma, ",", Trivia.OneSpace ) );
            }
            b.Add( new Token( TokenType.GenericIdentifier, named.ExportedName, Trivia.OneSpace ) );
            if( named.IsAliased )
            {
                b.Add( new Token( TokenType.GenericIdentifier, "as", Trivia.OneSpace ) );
                b.Add( new Token( TokenType.GenericIdentifier, named.ImportedName, Trivia.OneSpace ) );
            }
            if( needBraces )
            {
                b.Add( new Token( TokenType.CloseBrace, "}", Trivia.OneSpace ) );
                if( isSideEffectOnly )
                {
                    b.Add( new Token( TokenType.GenericIdentifier, "from", Trivia.OneSpace ) );
                }
            }
            using var e = editor.OpenGlobalEditor();
            e.Replace( tokenIndex, 1, b.ToArray() );
        }
    }

    int GetTokenIndexOfNamedIndex( TokenSpan span, int index )
    {
        int offset = span.Beg + 1; // import
        if( TypeOnly ) ++offset; // type
        if( DefaultImport != null ) ++offset; // DefaultImport
        Throw.DebugAssert( "Namespace excludes named imports.", Namespace == null );
        if( NamedImports.Count > 0 )
        {
            if( DefaultImport != null ) ++offset; //,
            ++offset; // {
            int idx = 0;
            foreach( var named in NamedImports )
            {
                if( idx == index ) return offset;
                if( idx++ > 0 ) ++offset; // ,
                if( named.TypeOnly ) ++offset;
                offset += named.IsAliased ? 3 : 1; // "A as B" or "A"
                if( idx == NamedImports.Count ) break;
            }
        }
        return offset;
    }

    void MonoTokenUpdate( SourceCodeEditor editor, TokenSpan span )
    {
        var currentToken = editor.Code.Tokens[span.Beg];
        Token newToken = new Token( currentToken.TokenType, currentToken.LeadingTrivias, ToString(), currentToken.TrailingTrivias );
        using var e = editor.OpenGlobalEditor();
        e.Replace( span.Beg, newToken );
    }

    /// <summary>
    /// Writes the import line.
    /// </summary>
    /// <param name="b">The target builder.</param>
    /// <returns>The builder.</returns>
    public StringBuilder Write( StringBuilder b )
    {
        b.Append( "import " );
        if( TypeOnly ) b.Append( "type " );
        if( DefaultImport != null ) b.Append( DefaultImport );
        if( Namespace != null )
        {
            b.Append( DefaultImport != null ? ", * as " : "* as " ).Append( Namespace );
        }
        if( NamedImports.Count > 0 )
        {
            if( DefaultImport != null || Namespace != null ) b.Append( ", " );
            b.Append( "{ " );
            bool atLeastOne = false;
            foreach( var namedImport in NamedImports )
            {
                if( atLeastOne ) b.Append( ", " );
                atLeastOne = true;
                b.Append( namedImport.ExportedName );
                if( namedImport.IsAliased ) b.Append( " as " ).Append( namedImport.ImportedName );
            }
            b.Append( " } " );
        }
        else if( DefaultImport != null || Namespace != null ) 
        {
            b.Append( ' ' );
        }
        if( !SideEffectOnly ) b.Append( "from " );
        b.Append( '\'' ).Append( ImportPath ).Append( "\';" );
        return b;
    }

    /// <summary>
    /// Overridden to return the import line.
    /// </summary>
    /// <returns>The import line.</returns>
    public override string ToString() => Write( new StringBuilder() ).ToString();

}
