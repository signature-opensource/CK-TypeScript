using CK.Transform.Core;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading;

namespace CK.Core;

sealed partial class TransformEnvironment // TargetFinder
{
    internal bool FindTarget( IActivityMonitor monitor, FunctionSource source, TransformerFunction f, out ITransformable? target )
    {
        if( f.Language.IsTransformLanguage )
        {
            target = FindFunctionTarget( monitor, source, f );
            return target != null;
        }
        return FindTransformableItemTarget( monitor, source, f, out target );
    }

    TFunction? FindFunctionTarget( IActivityMonitor monitor, FunctionSource source, TransformerFunction f )
    {
        if( string.IsNullOrEmpty( f.Target ) )
        {
            monitor.Error( $"""
                    Transformer of transformer in {source.Origin} must specify a target name.
                    Missing on "..." clause in:
                    {f.Text}
                    """ );
            return null;
        }
        if( _transformFunctions.TryGetValue( f.Target, out var target ) )
        {
            if( source.Resources.Reachables.Contains( target.Source.Resources.Package ) )
            {
                return target;
            }
            monitor.Error( $"""
                            Unreachable target '{f.Target}' in {source.Origin} for:
                            {f.Text}

                            {DumpReachables( source, _transformFunctions )}

                            Target '{f.Target}' is in {target.Source.Origin}.
                            """ );
        }
        else
        {
            monitor.Error( $"""
                            Unable to find target '{f.Target}' in {source.Origin} for:
                            {f.Text}

                            {DumpReachables( source, _transformFunctions )}
                            """ );
        }
        return null;

        static string DumpReachables( FunctionSource source, Dictionary<string, TFunction> functions ) => $"""
                            From {source.Origin}, the reachable transformers are:
                            {functions.Values.Where( t => source.Resources.Reachables.Contains( t.Source.Resources.Package ) )
                                         .GroupBy( t => t.Source.Resources )
                                         .OrderBy( g => g.Key.Index )
                                         .Select( g => $"""
                                                               {g.Key}:
                                                                   {g.Select( t => t.FunctionName ).Concatenate( "    " + Environment.NewLine )}
                                                               
                                                               """ )}
                            """;
    }

    bool FindTransformableItemTarget( IActivityMonitor monitor,
                                      FunctionSource source,
                                      TransformerFunction f,
                                      out ITransformable? result )
    {
        GetTargetNameToFind( source, f, out var expectedPath, out var isNamePrefix, out var name );
        if( name.Length == 0 )
        {
            monitor.Error( $"""
                Unable to derive a target name for {source.Origin} transformer:
                {f.Text}
                """ );
            result = null;
            return false;
        }
        if( expectedPath.StartsWith( "../" ) )
        {
            result = null;
            if( _externalItemResolver != null )
            {
                var extItem = _externalItemResolver.Resolve( monitor, f, expectedPath, isNamePrefix, name );
                if( extItem == null )
                {
                    return false;
                }
                result = new ExternalItem( extItem );
            }
            else
            {
                if( !IsLive )
                {
                    monitor.Error( $"A transform function in {source.Origin} targets an external item '{f.Target}' but no {nameof( IExternalTransformableItemResolver )} has been configured." );
                    return false;
                }
                monitor.Warn( $"A transform function in {source.Origin} targets an external item '{f.Target}'. This is ignored in Live mode." );
            }
            return true;
        }
        result = FindTransformableItemsInReachableResources( monitor, source, f, expectedPath, isNamePrefix, name );
        return result != null;
    }

    static bool MatchName( bool isNamePrefix,
                           ReadOnlySpan<char> name,
                           ReadOnlySpan<char> candidateName )
    {
        if( isNamePrefix )
        {
            // There cannot be a name equals to namePrefix: the items are filtered by their
            // extensions: if they are here, their name has one of the extensions of the language.
            // So we only handle candidate names longer than the name with an expected following '.'.
            if( candidateName.Length > name.Length
                && candidateName[name.Length] == '.'
                && candidateName.StartsWith( name, StringComparison.Ordinal ) )
            {
                return true;
            }
        }
        else if( candidateName.Equals( name, StringComparison.Ordinal ) )
        {
            return true;
        }
        return false;
    }

    static bool MatchExpectedPath( string candidatePath, ReadOnlySpan<char> expectedPath, ReadOnlySpan<char> name )
    {
        // There may be no path at all if the item is mapped to the root.
        // We decrement the length by 1 to skip the latest / separator.
        var pathLen = candidatePath.Length - name.Length - 1;
        if( pathLen == expectedPath.Length
              && expectedPath.Equals( candidatePath.AsSpan( 0, pathLen ), StringComparison.Ordinal ) )
        {
            return true;
        }
        if( pathLen > expectedPath.Length )
        {
            var cPath = candidatePath.AsSpan( 0, pathLen );
            if( cPath[pathLen - expectedPath.Length - 1] == '/'
                && cPath.EndsWith( expectedPath, StringComparison.Ordinal ) )
            {
                return true;
            }
        }
        return false;
    }

    static void GetTargetNameToFind( FunctionSource source,
                                     TransformerFunction f,
                                     out ReadOnlySpan<char> expectedPath,
                                     out bool isNamePrefix,
                                     out ReadOnlySpan<char> name )
    {
        var nameTofind = f.Target;
        if( string.IsNullOrWhiteSpace( nameTofind ) )
        {
            // No "on <target>" of the create transformer itself:
            // we use the name of the source (the file name that contains
            // the create transfomer without extensions) as a prefix of
            // the target name.
            // There is no expected path.
            expectedPath = default;
            name = source.SourceName;
            isNamePrefix = true;
            return;
        }
        // The function target may contain a sub path: it is an expected path.
        name = nameTofind.AsSpan();
        int idx = name.LastIndexOf( '/' );
        if( idx >= 0 )
        {
            // Removes any leading /.
            expectedPath = name.Slice( 0, idx ).TrimStart( '/' );
            name = name.Slice( idx + 1 );
        }
        else
        {
            expectedPath = default;
        }
        // If the function's target ends with an extension, it is an exact name,
        // otherwise we consider it as a name prefix.
        var ext = f.Language.TransformLanguage.CheckLangageFilename( name );
        isNamePrefix = ext.Length == 0;
    }

    TransformableItem? FindTransformableItemsInReachableResources( IActivityMonitor monitor,
                                                                   FunctionSource source,
                                                                   TransformerFunction f,
                                                                   ReadOnlySpan<char> expectedPath,
                                                                   bool isNamePrefix,
                                                                   ReadOnlySpan<char> name )
    {
        TransformableItem? best = null;
        List<TransformableItem>? ambiguities = null;
        foreach( var p in source.Resources.Reachables )
        {
            foreach( var candidate in _tracker.GetItems( p, f.Language.Index ) )
            {
                // This may be externalized in a strategy (configured by a TransformableFileHandler
                // constructor parameter).
                // Currently, this has been designed to:
                // - handle a "Less" transformer to find a ".css" or a ".less" item (exact file
                //   extensions are "erased" unless the transformer on "target" specifies an extension).
                // - handle a "top-bar.t" to locate a "top-bar.component.ts" item (thanks to the name prefix
                //   matching).
                // - Enables a transformer on "target" to disambiguate items thanks to the expectedPath.
                //   given 2 items "CK/Ng/AXIOSToken.ts" and "Partner/AXIOSToken.ts", a
                //   create typescript transformer on "Ng/AXIOSToken" will do the job.
                //   This may be enhanced ("CK/AXIOSToken.ts" - by the start, or by introducing globbing)
                //   if needed.
                //
                //   This doesn't handle "ambient hints" (and it seems not easy to honor them) like an
                //   ambient sql schema that will find a "CK.sUserRead.sql" item from a "sUserRead" target
                //   name. This is where an external (optional) strategy can handle this. 
                //
                var candidateName = candidate.TargetPath.LastPart.AsSpan();
                if( MatchName( isNamePrefix, name, candidateName ) )
                {
                    // Name matches, we must now handle the expectedPath.
                    if( expectedPath.Length == 0
                        || MatchExpectedPath( candidate.TargetPath.Path, expectedPath, name ) )
                    {
                        if( best == null )
                        {
                            best = candidate;
                        }
                        else
                        {
                            ambiguities ??= new List<TransformableItem>();
                            ambiguities.Add( candidate );
                        }
                    }
                }
            }
        }
        if( best == null || ambiguities != null )
        {
            using( monitor.OpenError( $"""
                            Unable to find the target for {source.Origin} transformer:
                            {f.Text}
                            """ ) )
            {
                var n = $"{(isNamePrefix ? "NamePrefix" : "ExactName")}: '{name}'";
                monitor.Error( $"Considering: {n}{(expectedPath.Length > 0 ? $", with expected path '{expectedPath}'" : "")}." );
                if( ambiguities != null )
                {
                    monitor.Error( $"""
                                Found {ambiguities.Count} ambiguous candidates:
                                {ambiguities.Select( i => i.TargetPath.Path ).Concatenate( Environment.NewLine )}
                                """ );

                    best = null;
                }
            }
        }
        return best;
    }

}
