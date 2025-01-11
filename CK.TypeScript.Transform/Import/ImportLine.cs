using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    /// <param name="Type">Whether this is a type only import.</param>
    public readonly record struct NamedImport( string ExportedName, string? ImportedName, bool Type )
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

    IReadOnlyList<NamedImport> IImportLine.NamedImports => NamedImports;

    internal void RemoveTypeOnly( SourceCodeEditor editor, TokenSpan span )
    {
        Throw.DebugAssert( TypeOnly );
        editor.RemoveAt( span.Beg + 1 );
        TypeOnly = false;
    }

    internal void SetNamedImportType( SourceCodeEditor editor, TokenSpan span, int index, bool set )
    {
        Throw.DebugAssert( index >= 0 && index < NamedImports.Count );
        var n = NamedImports[index];
        Throw.DebugAssert( n.Type != set );

        // Updates the NamedImport.Type flag.
        NamedImports[index] = new NamedImport( n.ExportedName, n.ImportedName, set );
        if( span.Length == 1 )
        {
            var currentToken = editor.SourceCode.Tokens[span.Beg];
            Token newToken = new Token( currentToken.TokenType, currentToken.LeadingTrivias, ToString(), currentToken.TrailingTrivias );
            editor.InPlaceReplace( span.Beg, newToken );
        }
        else
        {
            int offset = 2; // import {
            if( TypeOnly ) ++offset; // type
            if( DefaultImport != null ) offset += 2; // DefaultImport,
            if( Namespace != null ) offset += 2; // Namespace,
            int idx = 0;
            foreach( var named in NamedImports )
            {
                if( idx == index ) break;
                if( named.Type ) ++offset;
                offset += n.IsAliased ? 4 : 2; // A as B, or A,
                ++idx;
            }
            if( set )
            {
                Token newToken = new Token( TokenType.GenericIdentifier, "type", Trivia.OneSpace );
                editor.InsertAt( span.Beg + offset, newToken );
            }
            else
            {
                editor.RemoveAt( span.Beg + offset );
            }
        }
    }

    public StringBuilder Write( StringBuilder b, out int tokenCount )
    {
        // Account for "import", importPath and ";".
        tokenCount = 3;
        b.Append( "import " );
        if( TypeOnly )
        {
            tokenCount++;
            b.Append( "type " );
        }
        if( DefaultImport != null )
        {
            tokenCount++;
            b.Append( DefaultImport );
        }

        if( Namespace != null )
        {
            if( DefaultImport != null )
            {
                tokenCount++;
                b.Append( ", " );
            }
            tokenCount++;
            b.Append( Namespace );
        }
        if( NamedImports.Count > 0 )
        {
            if( DefaultImport != null || Namespace != null )
            {
                tokenCount++;
                b.Append( ", " );
            }

            tokenCount++;
            b.Append( "{ " );
            bool atLeastOne = false;
            foreach( var namedImport in NamedImports )
            {
                if( atLeastOne )
                {
                    tokenCount++;
                    b.Append( ", " );
                }

                atLeastOne = true;
                tokenCount++;
                b.Append( namedImport.ExportedName );
                if( namedImport.IsAliased )
                {
                    tokenCount++;
                    b.Append( " as " ).Append( namedImport.ImportedName );
                }
            }
            tokenCount++;
            b.Append( " } " );
        }
        else if( DefaultImport != null || Namespace != null ) 
        {
            b.Append( ' ' );
        }
        if( !SideEffectOnly )
        {
            tokenCount++;
            b.Append( "from " );
        }
        b.Append( '\'' ).Append( ImportPath ).Append( "\';" );
        return b;
    }

    public override string ToString() => Write( new StringBuilder(), out _ ).ToString();

}
