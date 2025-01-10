using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace CK.TypeScript.Transform;

/// <summary>
/// Mutable <see cref="IImportLine"/>.
/// </summary>
public sealed class ImportLine : IImportLine
{
    public ImportLine()
    {
        NamedImports = new List<NamedImport>();
        ImportPath = string.Empty;
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

    public string ImportPath { get; set; }

    IReadOnlyList<NamedImport> IImportLine.NamedImports => NamedImports;

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
