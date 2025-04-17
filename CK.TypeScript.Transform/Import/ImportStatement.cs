using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;

namespace CK.TypeScript.Transform;

/// <summary>
/// Captures the import statement.
/// </summary>
/// <remarks>
/// These are valid patterns:
/// <list type="bullet">
///     <item><c>import './type-system.model';</c> (side effetcs only)</item>
///     <item><c>import { IUserInfoType, StdKeyType } from './type-system.model';</c> (named imports)</item>
///     <item><c>import { } from './type-system.model';</c> (useless but valid)</item>
///     <item><c>import type { IUserInfoType, StdKeyType } from './type-system.model';</c> (type only imports)</item>
///     <item><c>import { type IUserInfoType, StdKeyType } from './type-system.model';</c> (mixed imports)</item>
///     <item><c>import { type IUserInfoType as U, StdKeyType } from './type-system.model';</c> (mixed imports with name alias)</item>
///     <item><c>import DEFAULT_IMPORT from './type-system.model';</c> (default import)</item>
///     <item><c>import type DEFAULT_IMPORT from './type-system.model';</c> (default type only import)</item>
///     <item><c>import DEFAULT_IMPORT, { IUserInfoType, StdKeyType } from './type-system.model';</c></item>
///     <item><c>import DEFAULT_IMPORT, * as NAMESPACE from './type-system.model'; (rather stupid but valid)</c></item>
///     <item><c>import type * as NAMESPACE from './type-system.model';</c></item>
///     <item><c>import * as NAMESPACE from './type-system.model';</c></item>
/// </list> 
/// Following are invalid patterns:
/// <list type="bullet">
///     <item><c>import from './type-system.model';</c></item>
///     <item><c>import type { type IUserInfoType, StdKeyType } from './type-system.model';</c></item>
///     <item><c>import * from './type-system.model';</c></item>
///     <item><c>import * as NAMESPACE, { IUserInfoType } from './type-system.model';</c></item>
///     <item><c>import * as NAMESPACE, DEFAULT_IMPORT from './type-system.model';</c></item>
///     <item><c>import { IUserInfoType }, DEFAULT_IMPORT from './type-system.model';</c></item>
///     <item><c>import { IUserInfoType }, * as NAMESPACE_DEFINITION from './type-system.model';</c></item>
///     <item><c>import type DEFAULT_IMPORT, { IUserInfoType, StdKeyType } from './type-system.model';</c></item>
///     <item><c>import type DEFAULT_IMPORT, * as NAMESPACE from './type-system.model';</c></item>
///     <item><c>import DEFAULT_IMPORT, * as NAMESPACE_DEFINITION, { IUserInfoType, StdKeyType } from './type-system.model';</c></item>
/// </list>
/// </remarks>
public sealed class ImportStatement : SourceSpan, IImportLine
{
    internal readonly ImportLine _line;

    internal ImportStatement( int beg, int end, ImportLine line )
        : base( beg, end )
    {
        _line = line;
    }

    /// <inheritdoc />
    public bool SideEffectOnly => _line.SideEffectOnly;

    /// <inheritdoc />
    public bool TypeOnly => _line.TypeOnly;

    /// <inheritdoc />
    public string? Namespace => _line.Namespace;

    /// <inheritdoc />
    public string? DefaultImport => _line.DefaultImport;

    /// <inheritdoc />
    public IReadOnlyList<ImportLine.NamedImport> NamedImports => ((IImportLine)_line).NamedImports;

    /// <inheritdoc />
    public string ImportPath { get => _line.ImportPath; set => _line.ImportPath = value; }

    internal void RemoveTypeOnly( SourceCodeEditor editor ) => _line.RemoveTypeOnly( editor, Span );

    internal void SetNamedImportType( SourceCodeEditor editor, int index, bool set ) => _line.SetNamedImportType( editor, Span, index, set );

    internal void AddNamedImport( SourceCodeEditor editor, ImportLine.NamedImport named ) => _line.AddNamedImport( editor, Span, named );

    internal static ImportStatement? TryMatch( Token importToken, ref TokenizerHead head )
    {
        Throw.DebugAssert( importToken.Text.Span.Equals( "import", StringComparison.Ordinal ) );
        int begImport = head.LastTokenIndex;
        var d = new ImportLine();

        // Type only import. There should be a default import or named imports but not both.
        // Here we accept both.
        d.TypeOnly = head.TryAcceptToken( "type", out var _ );

        // If there is a default import, there can be a namespace definition after.
        // The following is valid but redundant:
        //      import DefImport, * as NS from '@angular/core';
        //
        // But after a namespace declaration, no default import is allowed. This is invalid:
        //      import * as NS, DefImport from '@angular/core';
        //
        // => We start by the default import and continue with the namespace and named imports
        //    (and allow potentially invalid constructs).
        //
        if( head.TryAcceptToken( TokenType.GenericIdentifier, out var defaultImport ) )
        {
            // Eats the potential comma.
            head.TryAcceptToken( TokenType.Comma, out var _ );
            d.DefaultImport = defaultImport.ToString();
        }

        if( head.TryAcceptToken( TokenType.Asterisk, out var _ ) )
        {
            // "import * from ..." is invalid: we must have "as <identifier>".
            if( head.MatchToken( "as" ) is TokenError
                || head.LowLevelTokenType is not TokenType.GenericIdentifier )
            {
                return null;
            }
            d.Namespace = head.AcceptLowLevelToken().ToString();
        }
        // Namespace excludes named imports. But we always parse named imports.
        if( head.TryAcceptToken( TokenType.OpenBrace, out _ ) )
        {
            // Use while() instead of do-while because this is valid: "import { } from '...';"
            while( !head.TryAcceptToken( TokenType.CloseBrace, out _ ) )
            {
                // "import type { type X }..." is invalid but we don't care.
                bool isType = head.TryAcceptToken( "type", out _ ) || d.TypeOnly;
                Token exportedName = head.MatchToken( TokenType.GenericIdentifier, "exported symbol name" );
                if( exportedName is TokenError ) return null;
                Token? importedName = null;
                if( head.TryAcceptToken( "as", out _ ) )
                {
                    importedName = head.MatchToken( TokenType.GenericIdentifier, "imported symbol name" );
                    if( importedName is TokenError ) return null;
                }
                d.NamedImports.Add( new ImportLine.NamedImport( exportedName.ToString(), importedName?.ToString(), isType ) );
                // Eat the comma.
                head.TryAcceptToken( TokenType.Comma, out _ );
            }
        }
        // The "from" doesn't appear in a side-effect only import. 
        head.TryAcceptToken( "from", out _ );
        var importPath = head.MatchToken( TokenType.GenericString, "import path" );
        if( importPath is TokenError ) return null;
        d.ImportPath = importPath.Text.Slice( 1, importPath.Text.Length - 2 ).ToString();
        head.TryAcceptToken( ";", out _ );
        return new ImportStatement( begImport, head.LastTokenIndex + 1, d );
    }

}
