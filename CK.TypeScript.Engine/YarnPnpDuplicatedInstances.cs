using CK.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CK.Setup;

/// <summary>
/// Warns when a package of the application's runtime is installed as more than one instance by Yarn Plug'n'Play.
/// <para>
/// <b>Why.</b> Under Plug'n'Play, a package with peer dependencies gets a "virtual instance" per set of peers it receives.
/// When a library depends on a package with peers but doesn't declare these peers itself (ng-zorro-antd uses
/// @angular/cdk, whose peer rxjs it doesn't declare), the package reached through the library receives other peers
/// than the application's one: Yarn creates a second instance and the application runs two copies of it (two
/// overlay containers, directives that don't find their parent across the copies, a TestBed provider that applies
/// to one copy only). Yarn only says "YN0086: Some peer dependencies are incorrectly met by dependencies", that nobody
/// reads. The fix is a <c>[YarnPackageExtension]</c> (a peer added to the library) on the package that brings the library:
/// this check finds the next case of this kind before it becomes a runtime bug.
/// </para>
/// <para>
/// <b>How, and why this way.</b>
/// <list type="bullet">
///   <item>
///   The cost matters: a TypeScript generation is already long on a big project. The check runs only after a
///   <c>yarn install</c> (when the install marker says that nothing changed, there is no install and no check) and
///   reads the <c>.pnp.cjs</c> file that the install just wrote, without starting a process: about 13 ms on a full
///   Angular + Zorro application (900 KB, 950 locators). Running <c>yarn explain peer-requirements</c> instead would cost a
///   second (a Yarn process) and would report every unmet peer, most of them harmless.
///   </item>
///   <item>
///   The duplicates are searched in the runtime graph only: the locators reached from the application's
///   <c>dependencies</c> (and its peers), following the dependencies of each locator (the peers that a virtual instance
///   receives included), at any depth. A package reached only through its libraries is checked as well as a direct
///   dependency: npm packages are full of indirect dependencies. The build and test tooling (the <c>devDependencies</c>)
///   is not checked: duplicates are normal there (Babel plugins, Jest), and harmless. On the applications of the stack,
///   the runtime graph is 25 to 30 locators out of about 950, and has no duplicate once the ng-zorro-antd peer is declared.
///   </item>
///   <item>
///   It is a warning, not an error: a library may legitimately need another instance of a package with peers, and
///   failing the generation of an application on a library nobody looked at yet would be too strong. The warning names
///   the package, its instances and the packages that use each one, and says what to do.
///   </item>
///   <item>
///   The format of <c>.pnp.cjs</c> (the <c>packageRegistryData</c> of its <c>RAW_RUNTIME_STATE</c>, one entry per line) is
///   not a documented API: when it is not recognized (another Yarn version, another linker), the check says so in a trace
///   and does nothing. It never fails the generation.
///   </item>
/// </list>
/// </para>
/// </summary>
static class YarnPnpDuplicatedInstances
{
    /// <summary>
    /// A package installed as more than one instance in the runtime graph.
    /// </summary>
    /// <param name="Name">The package name.</param>
    /// <param name="Instances">Each instance (its displayed reference) and the packages that use it.</param>
    internal sealed record Duplicate( string Name, IReadOnlyList<(string Reference, IReadOnlyList<string> UsedBy)> Instances );

    /// <summary>
    /// Checks the <c>.pnp.cjs</c> of the target project and warns about the duplicated instances of its runtime graph.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="targetProjectPath">The target project (the folder of the <c>.pnp.cjs</c>).</param>
    /// <param name="runtimeDependencies">The names of the application's dependencies and peer dependencies.</param>
    internal static void Check( IActivityMonitor monitor, NormalizedPath targetProjectPath, IEnumerable<string> runtimeDependencies )
    {
        var pnp = targetProjectPath.AppendPart( ".pnp.cjs" );
        if( !File.Exists( pnp ) )
        {
            monitor.Trace( "No '.pnp.cjs' (the Yarn linker is not Plug'n'Play): the duplicated instances are not checked." );
            return;
        }
        IReadOnlyList<Duplicate>? duplicates;
        try
        {
            duplicates = Find( File.ReadLines( pnp ), runtimeDependencies );
        }
        catch( Exception ex )
        {
            monitor.Trace( "Unable to read the '.pnp.cjs' file: the duplicated instances are not checked.", ex );
            return;
        }
        if( duplicates == null )
        {
            monitor.Trace( "The format of the '.pnp.cjs' file is not recognized: the duplicated instances are not checked." );
            return;
        }
        foreach( var d in duplicates )
        {
            monitor.Warn( $"""
                Package '{d.Name}' is installed as {d.Instances.Count} instances by Yarn Plug'n'Play: the application runs {d.Instances.Count} copies of it.
                {d.Instances.Select( i => $"- {i.Reference}, used by {i.UsedBy.Concatenate()}." ).Concatenate( Environment.NewLine )}
                A package that uses '{d.Name}' probably doesn't declare one of its peers ("yarn explain peer-requirements" lists the unmet ones):
                declare it with a [YarnPackageExtension] (the peer added to that package) on the TypeScript package that brings it.
                """ );
        }
    }

    /// <summary>
    /// Finds the packages that have more than one instance in the runtime graph of a <c>.pnp.cjs</c> file.
    /// </summary>
    /// <param name="pnpLines">The lines of the <c>.pnp.cjs</c> file.</param>
    /// <param name="runtimeDependencies">The names of the application's dependencies and peer dependencies.</param>
    /// <returns>The duplicates (empty when there is none) or null when the format is not recognized.</returns>
    internal static IReadOnlyList<Duplicate>? Find( IEnumerable<string> pnpLines, IEnumerable<string> runtimeDependencies )
    {
        var graph = Parse( pnpLines );
        var root = graph.Keys.FirstOrDefault( k => k.Reference == "workspace:." );
        if( root.Name == null ) return null;
        var runtime = new HashSet<string>( runtimeDependencies );
        // The runtime closure, with the dependents of each locator.
        var usedBy = new Dictionary<(string Name, string Reference), List<(string Name, string Reference)>>();
        var queue = new Queue<(string Name, string Reference)>();
        foreach( var d in graph[root] )
        {
            if( runtime.Contains( d.Name ) ) Reach( d, root );
        }
        while( queue.Count > 0 )
        {
            var l = queue.Dequeue();
            if( graph.TryGetValue( l, out var deps ) )
            {
                // A package lists itself in its dependencies.
                foreach( var d in deps ) if( d != l ) Reach( d, l );
            }
        }
        return usedBy.Keys.Where( l => l.Reference.StartsWith( "virtual:", StringComparison.Ordinal ) )
                          .GroupBy( l => l.Name )
                          .Where( g => g.Count() > 1 )
                          .OrderBy( g => g.Key, StringComparer.Ordinal )
                          .Select( g => new Duplicate( g.Key,
                                                       g.Select( l => (Display( l.Reference ),
                                                                       (IReadOnlyList<string>)usedBy[l].Select( u => u == root
                                                                                                                       ? "the application"
                                                                                                                       : $"{u.Name}@{Display( u.Reference )}" )
                                                                                                         .ToList()) )
                                                        .ToList() ) )
                          .ToList();

        void Reach( (string Name, string Reference) l, (string Name, string Reference) from )
        {
            if( !usedBy.TryGetValue( l, out var list ) )
            {
                usedBy.Add( l, list = new List<(string Name, string Reference)>() );
                queue.Enqueue( l );
            }
            if( !list.Contains( from ) ) list.Add( from );
        }
    }

    // "virtual:e2df7ef7...#npm:22.2.6" is displayed like Yarn does: "npm:22.2.6 [e2df7]".
    static string Display( string reference )
    {
        if( !reference.StartsWith( "virtual:", StringComparison.Ordinal ) ) return reference;
        int sharp = reference.IndexOf( '#' );
        return sharp < 0 ? reference : $"{reference.Substring( sharp + 1 )} [{reference.Substring( 8, Math.Min( 5, sharp - 8 ) )}]";
    }

    // The packageRegistryData of the RAW_RUNTIME_STATE: a locator (name, reference) and its dependencies.
    //    ["@angular/cdk", [\
    //      ["virtual:e2df7...#npm:22.2.6", {\
    //        "packageLocation": "...",\
    //        "packageDependencies": [\
    //          ["@angular/cdk", "virtual:e2df7...#npm:22.2.6"],\
    //          ["rxjs", "npm:7.8.2"],\
    //          ["@types/angular__core", null],\          (an unmet optional peer)
    //          ["alias", ["real-name", "npm:1.0.0"]],\   (an aliased dependency)
    //        ],\
    static Dictionary<(string Name, string Reference), List<(string Name, string Reference)>> Parse( IEnumerable<string> lines )
    {
        var graph = new Dictionary<(string Name, string Reference), List<(string Name, string Reference)>>();
        string? name = null;
        List<(string Name, string Reference)>? current = null;
        bool inDependencies = false;
        foreach( var line in lines )
        {
            // The end of the RAW_RUNTIME_STATE: the rest of the file is the runtime code.
            if( line.StartsWith( "}'", StringComparison.Ordinal ) ) break;
            if( inDependencies )
            {
                if( line.StartsWith( "          [\"", StringComparison.Ordinal ) )
                {
                    if( TryReadString( line, 11, out var depName, out int next ) && current != null )
                    {
                        int i = next;
                        while( i < line.Length && line[i] is ',' or ' ' ) i++;
                        if( i < line.Length && line[i] == '[' )
                        {
                            // An alias: the locator is the real package.
                            if( TryReadString( line, i + 1, out var realName, out next ) )
                            {
                                i = next;
                                while( i < line.Length && line[i] is ',' or ' ' ) i++;
                                if( TryReadString( line, i, out var reference, out _ ) ) current.Add( (realName, reference) );
                            }
                        }
                        else if( TryReadString( line, i, out var reference, out _ ) )
                        {
                            current.Add( (depName, reference) );
                        }
                    }
                }
                else if( line.StartsWith( "        ]", StringComparison.Ordinal ) )
                {
                    inDependencies = false;
                }
            }
            else if( line.StartsWith( "    [", StringComparison.Ordinal ) && !line.StartsWith( "     ", StringComparison.Ordinal ) && line.EndsWith( "[\\", StringComparison.Ordinal ) )
            {
                name = TryReadString( line, 5, out var n, out _ ) ? n : null;
                current = null;
            }
            else if( line.StartsWith( "      [", StringComparison.Ordinal ) && !line.StartsWith( "       ", StringComparison.Ordinal ) && line.EndsWith( "{\\", StringComparison.Ordinal ) )
            {
                // The top-level locator [null, null] is skipped: the workspace has its own entry.
                current = name != null && TryReadString( line, 7, out var reference, out _ )
                            ? (graph[(name, reference)] = new List<(string Name, string Reference)>())
                            : null;
            }
            else if( current != null && line.StartsWith( "        \"packageDependencies\": [", StringComparison.Ordinal ) )
            {
                inDependencies = true;
            }
        }
        return graph;
    }

    // Reads a "..." string that starts at index start (a null or anything else is not a string).
    static bool TryReadString( string line, int start, out string value, out int next )
    {
        value = "";
        next = start;
        if( start >= line.Length || line[start] != '"' ) return false;
        int end = line.IndexOf( '"', start + 1 );
        if( end < 0 ) return false;
        value = line.Substring( start + 1, end - start - 1 );
        next = end + 1;
        return true;
    }
}
