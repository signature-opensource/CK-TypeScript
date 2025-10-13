using CK.Transform.Core;
using System.Collections.Generic;
using System;
using System.Linq;

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
                var e = new ExternalItem( extItem );
                // Okay, this is not really elegant but it's so easy to register the external
                // items here and, above all, safe because external items are only used once during
                // the initial setup and if somethig goes wrong later, the setup will be canceled
                // as a whole.
                _externalItems ??= new List<ExternalItem>();
                _externalItems.Add( e );
                result = e;
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
        if( FindTransformableItemsInReachableResources( monitor, source, f, expectedPath, isNamePrefix, name, out var item ) )
        {
            result = item;
            return true;
        }
        result = null;
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

    static bool MatchCandidatePath( ReadOnlySpan<char> expectedPath,
                                    bool isNamePrefix,
                                    ReadOnlySpan<char> name,
                                    ReadOnlySpan<char> candidatePath )
    {
        ReadOnlySpan<char> candidateName;
        int eoDir = candidatePath.LastIndexOf( '/' );
        candidateName = eoDir >= 0 ? candidatePath.Slice( eoDir + 1 ) : candidatePath;
        if( MatchName( isNamePrefix, name, candidateName ) )
        {
            if( expectedPath.Length == 0 ) return true;
            if( eoDir > 0 )
            {
                candidatePath = candidatePath.Slice( 0, eoDir );
                return MatchExpectedPath( candidatePath, expectedPath );
            }
        }
        return false;

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

        static bool MatchExpectedPath( ReadOnlySpan<char> candidatePath, ReadOnlySpan<char> expectedPath )
        {
            if( candidatePath.Length == expectedPath.Length
                  && expectedPath.Equals( candidatePath, StringComparison.Ordinal ) )
            {
                return true;
            }
            if( candidatePath.Length > expectedPath.Length )
            {
                if( candidatePath[candidatePath.Length - expectedPath.Length - 1] == '/'
                    && candidatePath.EndsWith( expectedPath, StringComparison.Ordinal ) )
                {
                    return true;
                }
            }
            return false;
        }
    }

    bool FindTransformableItemsInReachableResources( IActivityMonitor monitor,
                                                     FunctionSource source,
                                                     TransformerFunction f,
                                                     ReadOnlySpan<char> expectedPath,
                                                     bool isNamePrefix,
                                                     ReadOnlySpan<char> name,
                                                     out TransformableItem? result )
    {
        result = null;
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
                //   name. This is where an external (optional) strategy MAY handle this but it may not be a great idea:
                //   the detection of "non error" on eventurally optional package must work in the same way as if
                //   the package is not optional: any meta data (of the package) should not be used (or we'll
                //   have to marshall all the optional PackageDescriptor or at leats its Type so that attributes can
                //   be used... but what about data on the EngineAttributeImpl? Or worse, if the package is "manifest based"?
                //
                //   So it appears that pure name matching is a good thing: the eventually optional packages simply have to
                //   transmit their resource paths.
                //
                if( MatchCandidatePath( expectedPath, isNamePrefix, name, candidate.TargetPath.Path ) )
                {
                    if( result == null )
                    {
                        result = candidate;
                    }
                    else
                    {
                        ambiguities ??= new List<TransformableItem>();
                        ambiguities.Add( candidate );
                    }
                }
            }
        }
        if( result == null || ambiguities != null )
        {
            if( ambiguities == null )
            {
                Throw.DebugAssert( result == null );
                foreach( var p in _coreData.ExcludedOptionalResourcePaths )
                {
                    if( MatchCandidatePath( expectedPath, isNamePrefix, name, p ) )
                    {
                        monitor.Trace( $"Ignoring {source.Origin} transformer as it targets an eventually optional packages." );
                        return true;
                    }
                }
            }
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

                    result = null;
                }
                else
                {
                    var reachableItems = source.Resources.Reachables.SelectMany( p => _tracker.GetItems( p, f.Language.Index ) );
                    monitor.Error( $"""
                                Reachable transformable items (in language {f.Language.LanguageName}) are:
                                {reachableItems.Select( i => i.TargetPath.Path ).Concatenate( Environment.NewLine )}
                                """ );

                }
            }
        }
        return result != null;
    }

}
