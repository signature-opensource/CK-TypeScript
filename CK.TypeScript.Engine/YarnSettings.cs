using CK.Core;
using CK.TypeScript;
using CK.TypeScript.CodeGen;
using CSemVer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DependencyKind = CK.TypeScript.CodeGen.DependencyKind;

namespace CK.Setup;

/// <summary>
/// The Yarn settings declared by the packages: the <see cref="YarnPackageExtensionAttribute"/> (written in the
/// <c>packageExtensions</c> of the application's <c>.yarnrc.yml</c>) and the <see cref="PackageResolutionAttribute"/>
/// (written in the <c>resolutions</c> of the application's <c>package.json</c>).
/// <para>
/// The declarations are merged in the topological order of the packages: when two declarations have the same key,
/// two versions are merged by <see cref="PackageDependency.MergeVersionBound"/>, otherwise the last one wins.
/// </para>
/// </summary>
public sealed class YarnSettings
{
    /// <summary>
    /// The name of the <c>.yarnrc.yml</c> setting that this engine owns.
    /// </summary>
    public const string PackageExtensionsKey = "packageExtensions";

    /// <summary>
    /// A declared value: a range for an extension, a version for a resolution.
    /// </summary>
    public sealed class Declaration
    {
        internal Declaration( string value, string source, string? reason )
        {
            Value = value;
            Source = source;
            Reason = string.IsNullOrWhiteSpace( reason ) ? null : reason;
            var r = SVersionBound.NpmTryParse( value );
            if( r.IsValid ) Bound = r.Result;
        }

        /// <summary>
        /// Gets the declared value.
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Gets the version bound of the <see cref="Value"/> when it is a version range (not a protocol nor a tag).
        /// </summary>
        public SVersionBound? Bound { get; private set; }

        /// <summary>
        /// Gets the name of the type that declares the <see cref="Value"/>.
        /// </summary>
        public string Source { get; private set; }

        /// <summary>
        /// Gets the optional reason of the declaration.
        /// </summary>
        public string? Reason { get; private set; }

        internal bool Merge( IActivityMonitor monitor, string subject, Declaration other, bool ignoreVersionsBound )
        {
            if( Bound.HasValue && other.Bound.HasValue )
            {
                var v = Bound.Value;
                var s = Source;
                if( !PackageDependency.MergeVersionBound( monitor, subject, ref v, ref s, other.Bound.Value, other.Source,
                                                          ignoreVersionsBound, LogLevel.Trace, logWarn: true, out bool otherWins ) )
                {
                    return false;
                }
                if( otherWins ) Set( other );
            }
            else if( Value != other.Value )
            {
                monitor.Info( $"{subject}: '{other.Value}' (by '{other.Source}') replaces '{Value}' (by '{Source}'): the last declaration wins." );
                Set( other );
            }
            return true;
        }

        void Set( Declaration other )
        {
            Value = other.Value;
            Bound = other.Bound;
            Source = other.Source;
            Reason = other.Reason;
        }
    }

    /// <summary>
    /// A package extension: <see cref="Name"/> is added to the <see cref="Kind"/> of the packages that match the <see cref="Selector"/>.
    /// </summary>
    /// <param name="Selector">The extended package descriptor ("ng-zorro-antd@*").</param>
    /// <param name="Kind">The kind of the added edge (<see cref="DependencyKind.Dependency"/> or <see cref="DependencyKind.PeerDependency"/>).</param>
    /// <param name="Name">The added package.</param>
    /// <param name="Range">The range of the added package and its declaration.</param>
    public sealed record Extension( string Selector, DependencyKind Kind, string Name, Declaration Range );

    readonly List<Extension> _extensions;
    readonly Dictionary<string, Declaration> _resolutions;

    YarnSettings( List<Extension> extensions, Dictionary<string, Declaration> resolutions )
    {
        _extensions = extensions;
        _resolutions = resolutions;
    }

    /// <summary>
    /// Gets the package extensions, in the order of their first declaration.
    /// </summary>
    public IReadOnlyList<Extension> Extensions => _extensions;

    /// <summary>
    /// Gets the resolutions, in the order of their first declaration.
    /// </summary>
    public IReadOnlyDictionary<string, Declaration> Resolutions => _resolutions;

    /// <summary>
    /// Merges the declarations of the packages.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="extensions">The extensions in the topological order of the packages.</param>
    /// <param name="resolutions">The resolutions in the topological order of the packages.</param>
    /// <param name="ignoreVersionsBound">See <see cref="TypeScriptAspectConfiguration.IgnoreVersionsBound"/>.</param>
    /// <returns>The settings or null on error.</returns>
    internal static YarnSettings? Create( IActivityMonitor monitor,
                                          IEnumerable<(YarnPackageExtensionAttribute Attr, string Source)> extensions,
                                          IEnumerable<(PackageResolutionAttribute Attr, string Source)> resolutions,
                                          bool ignoreVersionsBound )
    {
        bool success = true;
        var extensionList = new List<Extension>();
        var extensionIndex = new Dictionary<(string, DependencyKind, string), Extension>();
        foreach( var (a, source) in extensions )
        {
            var kind = (DependencyKind)a.Kind;
            var d = new Declaration( a.Range, source, a.Reason );
            if( extensionIndex.TryGetValue( (a.Selector, kind, a.Name), out var exist ) )
            {
                success &= exist.Range.Merge( monitor, $"Yarn package extension '{a.Name}' of '{a.Selector}'", d, ignoreVersionsBound );
            }
            else
            {
                var e = new Extension( a.Selector, kind, a.Name, d );
                extensionIndex.Add( (a.Selector, kind, a.Name), e );
                extensionList.Add( e );
            }
        }
        success &= CheckOverlappingSelectors( monitor, extensionList );
        var resolutionMap = new Dictionary<string, Declaration>();
        foreach( var (a, source) in resolutions )
        {
            var d = new Declaration( a.Version, source, a.Reason );
            if( resolutionMap.TryGetValue( a.Pattern, out var exist ) )
            {
                success &= exist.Merge( monitor, $"Package resolution '{a.Pattern}'", d, ignoreVersionsBound );
            }
            else
            {
                resolutionMap.Add( a.Pattern, d );
            }
        }
        return success ? new YarnSettings( extensionList, resolutionMap ) : null;
    }

    static bool CheckOverlappingSelectors( IActivityMonitor monitor, List<Extension> extensions )
    {
        bool success = true;
        var groups = extensions.GroupBy( e => (Package: SplitSelector( e.Selector ).Name, e.Kind, Added: e.Name) )
                               .Where( g => g.Count() > 1 );
        foreach( var g in groups )
        {
            var all = g.ToList();
            for( int i = 0; i < all.Count; i++ )
            {
                for( int j = i + 1; j < all.Count; j++ )
                {
                    var r1 = SVersionBound.NpmTryParse( SplitSelector( all[i].Selector ).Range );
                    var r2 = SVersionBound.NpmTryParse( SplitSelector( all[j].Selector ).Range );
                    // A range that is not a version range can't be checked: it is considered to overlap.
                    // Two bounds overlap when the base of one of them satisfies the other one (the intersection of
                    // SVersionBound is an approximation: "^1" and "^2" don't intersect to None).
                    if( !r1.IsValid || !r2.IsValid || r1.Result.Satisfy( r2.Result.Base ) || r2.Result.Satisfy( r1.Result.Base ) )
                    {
                        monitor.Error( $"""
                            Yarn package extensions '{all[i].Selector}' (by '{all[i].Range.Source}') and '{all[j].Selector}' (by '{all[j].Range.Source}') both add the {g.Key.Kind} '{g.Key.Added}'
                            and their ranges overlap: Yarn applies both to the matching versions. The same selector must be used.
                            """ );
                        success = false;
                    }
                }
            }
        }
        return success;
    }

    /// <summary>
    /// Splits a selector "name@range" (the name can be scoped: "@scope/name@range").
    /// </summary>
    /// <param name="selector">The selector.</param>
    /// <returns>The name and the range. The range is empty when there is none.</returns>
    internal static (string Name, string Range) SplitSelector( string selector )
    {
        int idx = selector.IndexOf( '@', 1 );
        return idx < 0 ? (selector, "") : (selector.Substring( 0, idx ), selector.Substring( idx + 1 ));
    }

    /// <summary>
    /// Gets the <c>packageExtensions</c> block of the <c>.yarnrc.yml</c> file (with a final new line) or null when
    /// there is no package extension.
    /// </summary>
    /// <param name="newLine">The new line to use.</param>
    /// <returns>The block or null.</returns>
    public string? GetPackageExtensionsBlock( string newLine )
    {
        if( _extensions.Count == 0 ) return null;
        var b = new StringBuilder();
        b.Append( PackageExtensionsKey ).Append( ':' ).Append( newLine );
        foreach( var bySelector in _extensions.GroupBy( e => e.Selector ) )
        {
            b.Append( "  " ).Append( Quote( bySelector.Key ) ).Append( ':' ).Append( newLine );
            foreach( var byKind in bySelector.GroupBy( e => e.Kind ).OrderBy( g => g.Key ) )
            {
                b.Append( "    " )
                 .Append( byKind.Key == DependencyKind.PeerDependency ? "peerDependencies" : "dependencies" )
                 .Append( ':' ).Append( newLine );
                foreach( var e in byKind )
                {
                    var comment = e.Range.Reason == null ? e.Range.Source : $"{e.Range.Source}: {e.Range.Reason}";
                    foreach( var line in comment.Split( '\n' ) )
                    {
                        b.Append( "      # " ).Append( line.TrimEnd() ).Append( newLine );
                    }
                    b.Append( "      " ).Append( Quote( e.Name ) ).Append( ": " ).Append( Quote( e.Range.Value ) ).Append( newLine );
                }
            }
        }
        return b.ToString();

        static string Quote( string s ) => '"' + s.Replace( "\\", "\\\\" ).Replace( "\"", "\\\"" ) + '"';
    }

    /// <summary>
    /// Computes the content of a <c>.yarnrc.yml</c> file: its <c>packageExtensions</c> top-level setting is replaced
    /// with the <paramref name="packageExtensionsBlock"/> (or removed when it is null), the other settings are kept.
    /// </summary>
    /// <param name="current">The current content of the file. Null when there is no file.</param>
    /// <param name="packageExtensions">The settings or null: the <c>packageExtensions</c> setting is removed.</param>
    /// <returns>The new content. Null when there must be no file.</returns>
    public static string? UpdateYarnRc( string? current, YarnSettings? packageExtensions )
    {
        var newLine = current != null && current.Contains( "\r\n" ) ? "\r\n" : Environment.NewLine;
        var block = packageExtensions?.GetPackageExtensionsBlock( newLine );
        if( current == null ) return block;
        var lines = current.Split( '\n' ).Select( l => l.TrimEnd( '\r' ) ).ToList();
        // The last empty line is the end of the final new line.
        bool finalNewLine = lines.Count > 0 && lines[^1].Length == 0;
        if( finalNewLine ) lines.RemoveAt( lines.Count - 1 );
        int start = lines.FindIndex( l => l.StartsWith( PackageExtensionsKey + ':', StringComparison.Ordinal ) );
        if( start >= 0 )
        {
            // The setting ends before the first line that starts with a non white space (a column-0 comment
            // belongs to what follows). The trailing empty lines are left to what follows.
            int end = start + 1;
            while( end < lines.Count && (lines[end].Length == 0 || lines[end][0] is ' ' or '\t') ) end++;
            while( end > start + 1 && lines[end - 1].Trim().Length == 0 ) end--;
            lines.RemoveRange( start, end - start );
        }
        else
        {
            start = lines.Count;
        }
        if( block != null )
        {
            var blockLines = block.Split( newLine ).ToList();
            blockLines.RemoveAt( blockLines.Count - 1 );
            if( start == lines.Count && start > 0 && lines[^1].Trim().Length > 0 )
            {
                // Appended: separated from the previous setting by an empty line.
                blockLines.Insert( 0, "" );
            }
            lines.InsertRange( start, blockLines );
        }
        if( lines.All( l => l.Trim().Length == 0 ) ) return null;
        var result = string.Join( newLine, lines );
        if( finalNewLine || block != null ) result += newLine;
        return result;
    }
}
