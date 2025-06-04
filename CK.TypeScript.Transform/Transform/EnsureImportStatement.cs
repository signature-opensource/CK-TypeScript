using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CK.TypeScript.Transform;

/// <summary>
/// Ensure import statement.
/// </summary>
public sealed class EnsureImportStatement : TransformStatement
{
    internal EnsureImportStatement( int beg, int end )
        : base( beg, end )
    {
    }

    /// <summary>
    /// Checks that <see cref="ImportStatement"/> is valid.
    /// </summary>
    /// <returns>True if this span is valid.</returns>
    [MemberNotNullWhen( true, nameof(ImportStatement) )]
    public override bool CheckValid()
    {
        return base.CheckValid() && FirstChild is ImportStatement;
    }

    /// <summary>
    /// Gets the import statement.
    /// Never null when <see cref="CheckValid()"/> doesn't throw.
    /// </summary>
    public ImportStatement? ImportStatement => FirstChild as ImportStatement;

    /// <inheritdoc />
    public override void Apply( IActivityMonitor monitor, SourceCodeEditor editor )
    {
        Throw.DebugAssert( CheckValid() );
        EnsureImport( monitor, editor, ImportStatement );
    }

    /// <summary>
    /// Reusable implementation of <see cref="Apply(IActivityMonitor, SourceCodeEditor)"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="editor">The source code.</param>
    /// <param name="toImport">The import line to add.</param>
    public static void EnsureImport( IActivityMonitor monitor, SourceCodeEditor editor, IImportLine toImport )
    {
        // No need to respect any scope here. Imports are top-level statements.
        // Even if an import can appear anywhere in a file, it is not a good practice
        // and semantically useless.
        // No need to look for subordinated spans.

        // We work on a clone.
        var toMerge = new ImportLine( toImport );
        bool toMergeInitialSideEffectOnly = toMerge.SideEffectOnly;

        // This may handle existing default, namespace and side-effect only import.
        // On success, there's nothing more to do.
        // Otherwise, this produces an index of the ImportStatement by named imports
        // and the last import statement after which a new ImportStatement can be inserted.
        if( PreProcess( monitor, editor, toMerge, toMergeInitialSideEffectOnly,
                        out Dictionary<string, ImportStatement> existingNamedImports,
                        out ImportStatement? lastImport ) )
        {
            return;
        }

        // Now that the existingNamedImports index has been built, we can handle the named imports.
        // We unfortunately need 2 passes here: the first one to detect already imported and conflicts and
        // a second one to try to inject the named import into an existing import if possible.
        List<ImportLine.NamedImport>? handledNames = null;
        // First, removes from toMerge all the already imported names, checking that the imported name
        // doesn't conflict with an existing one.
        for( int i = 0; i < toMerge.NamedImports.Count; i++ )
        {
            ImportLine.NamedImport named = toMerge.NamedImports[i];
            if( existingNamedImports.TryGetValue( named.FinalName, out var exists ) )
            {
                // Same module: this is a serious candidate... IF it is not bound to another exported symbol.
                // And if it is not from the same Module, this is an error.
                if( exists.ImportPath == toMerge.ImportPath )
                {
                    // When found, the named is added to the handledNames.
                    AddToExistingStatement( monitor, editor, toImport, named, exists, ref handledNames );
                }
                else
                {
                    // The named.FinalName is already imported from another import path,
                    // this is an error...
                    // ...except if the new import path is more "precise" than the existing one.
                    // In such case, we remove the named import from the existing statement:
                    // a new statement will be created.
                    // This supports "barrel" removal and is used by '@local/ck-gen' types resolution
                    // but we let this work for any paths as it is not completely stupid and may be
                    // useful for other kind of import paths.
                    if( toMerge.ImportPath.Length > exists.ImportPath.Length + 1
                        && toMerge.ImportPath[exists.ImportPath.Length] == '/'
                        && toMerge.ImportPath.StartsWith( exists.ImportPath ) )
                    {
                        // The name necessarily exists.
                        var existingNameIdx = exists.NamedImports.IndexOf( n => n.FinalName == named.FinalName );
                        Throw.DebugAssert( existingNameIdx >= 0 );
                        var existingName = exists.NamedImports[existingNameIdx];
                        if( existingName.ExportedName != named.ExportedName )
                        {
                            monitor.Error( $"Cannot 'ensure {toImport.ToStringImport()}' because '{named}' conflicts with already imported '{existingName}' in '{exists.ToStringImport()}'." );
                        }
                        else
                        {
                            // Okay... We don't really care but this is right: if the new name is OnlyType
                            // and the existing one is not, we update our new name to be a regular import.
                            // (This is why we used a for instead of a foreach statement.)
                            if( named.TypeOnly && !existingName.TypeOnly )
                            {
                                toMerge.NamedImports[i] = named with { TypeOnly = false };
                            }
                            exists.RemoveNamedImport( editor, existingNameIdx );
                            // We had a name, so if now we are a SideEfectOnly import, we must disappear.
                            if( exists.SideEffectOnly )
                            {
                                editor.RemoveSpan( exists );
                                // If this was the last import statement, it is lost.
                                if( lastImport == exists )
                                {
                                    lastImport = null;
                                }
                            }
                        }
                    }
                    else
                    {
                        monitor.Error( $"Cannot 'ensure {toImport.ToStringImport()}' because '{named.FinalName}' is already imported from '{exists.ImportPath}'." );
                    }
                }
            }
        }
        // If an imported name conflicts, it is useless to continue.
        if( editor.HasError ) return;
        // Cleanup toMerge n°1/2.
        if( handledNames != null )
        {
            RemoveHandledNamedImports( toMerge, handledNames );
        }
        // Second, try to reuse an existing import to avoid a new import statement.
        foreach( var named in toMerge.NamedImports )
        {
            ImportStatement? best = null;
            foreach( var import in editor.Code.Spans.OfType<ImportStatement>() )
            {
                // Namespace excludes named imports.
                // Even if could merge a regular import in a TypeOnly statement without DefaultImport,
                // we avoid reverting the type only definition and prefer creating a new statement.
                if( toMerge.ImportPath == import.ImportPath
                    && import.Namespace == null
                    && !(import.TypeOnly && !named.TypeOnly) )
                {
                    if( import.DefaultImport == null )
                    {
                        // This is the best possible match: no DefaultImport.
                        best = import;
                        break;
                    }
                    else
                    {
                        // Keep the last one as a fallback.
                        best = import;
                    }
                }
            }
            if( best != null )
            {
                best.AddNamedImport( editor, named );
                // We are good.
                handledNames ??= new List<ImportLine.NamedImport>();
                handledNames.Add( named );
            }
        }
        // Cleanup toMerge n°2/2.
        if( handledNames != null )
        {
            RemoveHandledNamedImports( toMerge, handledNames );
        }
        if( !toMerge.SideEffectOnly || toMergeInitialSideEffectOnly )
        {
            // We create a token with the whole text (without the comments as they belong to the transform langage)
            // and inserts in the the source code (after the last import or at the beginning of the source).
            int insertionPoint = lastImport?.Span.End ?? 0;
            var importLine = toMerge.ToString();
            Token newText = new Token( TokenType.GenericAny,
                                       Trivia.Empty,
                                       importLine,
                                       Trivia.NewLine );
            using( var e = editor.OpenGlobalEditor() )
            {
                e.InsertBefore( insertionPoint, newText );
                // We then create a brand new (1 token length) ImportStatement with the toMerge line
                // and we add it to the spans.
                var newStatement = new ImportStatement( insertionPoint, insertionPoint + 1, toMerge );
                editor.AddSourceSpan( newStatement );
            }
        }

        static bool PreProcess( IActivityMonitor monitor,
                                SourceCodeEditor editor,
                                ImportLine toMerge,
                                bool toMergeInitialSideEffectOnly,
                                out Dictionary<string, ImportStatement> existingNamedImports,
                                out ImportStatement? lastImport )
        {
            // Indexed ImportStatement by named imports. 
            existingNamedImports = new Dictionary<string, ImportStatement>();
            lastImport = null;
            foreach( var import in editor.Code.Spans.OfType<ImportStatement>() )
            {
                // If we are ensuring a side-effect only "import '...';", we must just check that
                // the import path doesn't exist.
                if( toMergeInitialSideEffectOnly )
                {
                    if( import.ImportPath == toMerge.ImportPath )
                    {
                        // The path is imported. We are done.
                        return true;
                    }
                    continue;
                }
                // Regular import: first handles DefaultImport and Namespace for the module.
                if( import.ImportPath == toMerge.ImportPath )
                {
                    // We want a default import of the module.
                    if( toMerge.DefaultImport != null )
                    {
                        if( import.DefaultImport != null )
                        {
                            // This import is also a default import, if they are the same,
                            // we only have to handle the potential "type".
                            if( import.DefaultImport == toMerge.DefaultImport )
                            {
                                if( import.TypeOnly && !toMerge.TypeOnly )
                                {
                                    // We must remove the "type".
                                    // Note: there is no namespace or named imports:
                                    // - A type-only import can specify a default import or named bindings, but not both.ts(1363)
                                    import.RemoveTypeOnly( editor );
                                }
                                else
                                {
                                    // Same import [type] DEFAULT_EXPORT from 'path' or the existing
                                    // import is not a type only (it generalizes the incoming type only).
                                    // Nothing to do.
                                }
                                // We are done with the DefaultImport.
                                toMerge.DefaultImport = null;
                                // If it was the only thing to do, we are done.
                                if( toMerge.SideEffectOnly )
                                {
                                    return true;
                                }
                            }
                            else
                            {
                                monitor.Warn( $"Module '{import.ImportPath}' default export is already named '{import.DefaultImport}'. It will also be named '{toMerge.DefaultImport}'." );
                            }
                        }
                        else
                        {
                            // This import doesn't have a DefaultImport.
                            // If it has no namespace (and may be named imports), we could update it, but let's be lazy:
                            // a new import will be generated.
                        }
                    }
                    // We want to import the module as a namespace.
                    // There may be a DefaultIport but there are no named import here
                    // (import * as NAMESPACE, { A } from '...' is invalid).
                    if( toMerge.Namespace != null )
                    {
                        if( import.Namespace != null )
                        {
                            // This import has also a namespace, if they are the same,
                            // we only have to handle the potential "type".
                            if( import.Namespace == toMerge.Namespace )
                            {
                                if( import.TypeOnly && import.TypeOnly && !toMerge.TypeOnly )
                                {
                                    // We must remove the "type".
                                    // Note: there is no default import or named imports:
                                    // - A type-only import can specify a default import or named bindings, but not both.ts(1363)
                                    import.RemoveTypeOnly( editor );
                                }
                                else
                                {
                                    // Same import [type] * as Namespace from 'path' or the existing
                                    // import is not a type only (it generalizes the incoming type only).
                                    // Nothing to do.
                                }
                                // We are done with the Namespace.
                                toMerge.Namespace = null;
                                // If it was the only thing to do, we are done.
                                if( toMerge.SideEffectOnly )
                                {
                                    return true;
                                }
                            }
                            else
                            {
                                monitor.Warn( $"Module '{import.ImportPath}' is already imported as '{import.Namespace}'. It will also be imported as '{toMerge.Namespace}'." );
                            }
                        }
                        else
                        {
                            // This import doesn't have a namespace.
                            // If it has no named import (it may have a DefaultImport), we could update it, but let's be lazy:
                            // a new import will be generated.
                        }
                    }
                }
                // Then collects all the named imports.
                foreach( var named in import.NamedImports )
                {
                    if( existingNamedImports.TryGetValue( named.FinalName, out var existingImport ) )
                    {
                        // This is actually an error, but it comes from the original source code so we just warn.
                        monitor.Warn( $"Duplicate import '{named.FinalName}' found in '{import.ToStringImport()}' and '{existingImport.ToStringImport()}'." );
                    }
                    else
                    {
                        existingNamedImports.Add( named.FinalName, import );
                    }
                }
                lastImport = import;
            }
            return false;
        }

        static void RemoveHandledNamedImports( ImportLine toMerge, List<ImportLine.NamedImport> handledNames )
        {
            foreach( var n in handledNames ) toMerge.NamedImports.Remove( n );
            handledNames.Clear();
        }

        static void AddToExistingStatement( IActivityMonitor monitor,
                                            SourceCodeEditor editor,
                                            IImportLine toImport,
                                            ImportLine.NamedImport named,
                                            ImportStatement existing,
                                            ref List<ImportLine.NamedImport>? handledNames )
        {
            // We may need the index to handle "type".
            var existingNameIdx = existing.NamedImports.IndexOf( n => n.FinalName == named.FinalName );
            Throw.DebugAssert( existingNameIdx >= 0 );
            var existingName = existing.NamedImports[existingNameIdx];
            if( existingName.ExportedName != named.ExportedName )
            {
                monitor.Error( $"Cannot 'ensure {toImport.ToStringImport()}' because '{named}' conflicts with already imported '{existingName}'." );
            }
            else if( !editor.HasError )
            {
                // We must handle "type".
                // - If both imports have the same "type". Nothing to do.
                // - If the existing import is not a "type". Nothing to do.
                // - If the existing type is a "type" (and the toMerge one is not).
                //   We must remove the "type" from the existing one.
                if( existingName.TypeOnly && !named.TypeOnly )
                {
                    if( existing.TypeOnly )
                    {
                        // If the "type" is at the statement level, we can handle it
                        // easily only if it contains ONLY this import (by removing the "type" from the statement).
                        //
                        // Luckily, there is no namespace or named imports:
                        // - A type-only import can specify a default import or named bindings, but not both. ts(1363)
                        // And "type" cannot be on both levels:
                        // - The 'type' modifier cannot be used on a named import when 'import type' is used on its import statement. ts(2206)
                        //  
                        // When other named import exist, we MUST suffer... because a type import cannot coexist
                        // with regular ones.
                        // Given an exisiting:
                        //      import { A } from '...';
                        // None of these can be added to the file:
                        //      import type { A } from '...';
                        //      import { type A } from '...';
                        //
                        // So we must clear the statement type...
                        existing.RemoveTypeOnly( editor );
                        // ..and reinject a "type" for all other imports. 
                        if( existing.NamedImports.Count > 1 )
                        {
                            for( int i = 0; i < existing.NamedImports.Count; i++ )
                            {
                                if( i != existingNameIdx )
                                {
                                    existing.SetNamedImportType( editor, i, true );
                                }
                            }
                        }
                    }
                    else
                    {
                        // We must remove the "type".
                        existing.SetNamedImportType( editor, existingNameIdx, false );
                    }
                }
                // We are good.
                handledNames ??= new List<ImportLine.NamedImport>();
                handledNames.Add( named );
            }
        }
    }
}
