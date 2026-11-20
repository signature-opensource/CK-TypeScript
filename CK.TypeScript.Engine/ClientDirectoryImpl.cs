using CK.AppIdentity;
using CK.CodeGen;
using CK.Core;
using System;
using System.Linq;
using System.Reflection;

namespace CK.Setup;

/// <summary>
/// Code generator of the <see cref="CK.TypeScript.ClientDirectory"/> service: its client applications are the TypeScript
/// configurations of the BinPath that are client applications (<see cref="TypeScriptBinPathAspectConfiguration.Kind"/>).
/// <para>
/// The facts of a client application (<see cref="CK.TypeScript.ClientInfo.Facts"/>) are the union of the
/// <see cref="PartyClientFactAttribute"/> of its selected TypeScript packages: the generation waits for the package
/// selection of the <see cref="TypeScriptAspect"/>.
/// </para>
/// <para>
/// It also writes the "$StObjGen/Clients.props" of the host (<see cref="ClientsPropsFileName"/>): one
/// <c>CKClientApplication</c> item per client application (its folder, its built output, its yarn), that the publication
/// of the host reads to build them (the "buildTransitive" targets of CK.TS.AspNet.Clients).
/// </para>
/// </summary>
public sealed class ClientDirectoryImpl : CSCodeGeneratorType
{
    /// <summary>
    /// The MSBuild file of the client applications, in the "$StObjGen" folder of the host.
    /// </summary>
    public const string ClientsPropsFileName = "Clients.props";

    /// <inheritdoc />
    public override CSCodeGenerationResult Implement( IActivityMonitor monitor, Type classType, ICSCodeGenerationContext c, ITypeScope scope )
    {
        // Skips the purely unified BinPath.
        if( c.CurrentRun.ConfigurationGroup.IsUnifiedPure ) return CSCodeGenerationResult.Success;
        // The facts of the client applications need the package selection of the TypeScriptAspect. Without client
        // application (and maybe without TypeScript aspect), the directory is empty: nothing to wait for.
        return GetClients( c ).Count > 0
                ? new CSCodeGenerationResult( nameof( GenerateWithSelection ) )
                : Generate( monitor, c, scope, null );
    }

    CSCodeGenerationResult GenerateWithSelection( IActivityMonitor monitor, ICSCodeGenerationContext c, ITypeScope scope, [WaitFor] TypeScriptPackageSelection selection )
    {
        return Generate( monitor, c, scope, selection );
    }

    // The default client application is the last one: the host maps it after the other ones.
    static System.Collections.Generic.List<TypeScriptBinPathAspectConfiguration> GetClients( ICSCodeGenerationContext c )
    {
        return c.CurrentRun.ConfigurationGroup.Configuration.FindAspect<TypeScriptBinPathAspectConfiguration>()?.AllConfigurations
                    .Where( ts => ts.RequestPath != null )
                    .OrderBy( ts => ts.Kind == ClientKind.DefaultApp )
                    .ToList()
               ?? [];
    }

    CSCodeGenerationResult Generate( IActivityMonitor monitor, ICSCodeGenerationContext c, ITypeScope scope, TypeScriptPackageSelection? selection )
    {
        var binPath = c.CurrentRun.ConfigurationGroup.Configuration;
        var clients = GetClients( c );
        scope.Append( "public " ).Append( scope.Name ).Append( "() : base( new CK.TypeScript.ClientInfo[] {" ).NewLine();
        var items = new System.Text.StringBuilder();
        foreach( var ts in clients )
        {
            var projectPath = ts.TargetProjectPath.StartsWith( binPath.ProjectPath )
                                ? ts.TargetProjectPath.RemovePrefix( binPath.ProjectPath )
                                : ts.TargetProjectPath;
            var outputPath = new NormalizedPath( "dist" ).AppendPart( ts.TargetProjectPath.LastPart ).AppendPart( "browser" );
            // A client application has a name, and there is a selection when there are client applications.
            Throw.DebugAssert( selection != null );
            var facts = GetFacts( selection, ts.Name! );
            scope.Append( "new CK.TypeScript.ClientInfo( " )
                 .AppendSourceString( ts.Name ).Append( ", " )
                 .AppendSourceString( ts.RequestPath ).Append( ", " )
                 .AppendSourceString( projectPath.Path ).Append( ", " )
                 .AppendSourceString( outputPath.Path ).Append( ", " )
                 .AppendSourceString( ts.TypeFilterName ).Append( ", " )
                 .Append( ts.DevServerPort!.Value ).Append( ", " )
                 .Append( "[" );
            bool first = true;
            foreach( var (key, value) in facts.Pairs )
            {
                if( !first ) scope.Append( ", " );
                first = false;
                scope.Append( "(" ).AppendSourceString( key ).Append( ", " ).AppendSourceString( value ).Append( ")" );
            }
            scope.Append( "] )," ).NewLine();
            var yarn = FindYarn( ts.TargetProjectPath );
            items.Append( "    <CKClientApplication Include=\"" ).Append( Escape( projectPath.Path ) )
                 .Append( "\" Name=\"" ).Append( Escape( ts.Name! ) )
                 .Append( "\" OutputPath=\"" ).Append( Escape( outputPath.Path ) )
                 .Append( "\" Yarn=\"" ).Append( yarn == null ? "" : Escape( yarn.Value.StartsWith( binPath.ProjectPath ) ? yarn.Value.RemovePrefix( binPath.ProjectPath ).Path : yarn.Value.Path ) )
                 .Append( "\" />" ).Append( Environment.NewLine );
            monitor.Info( facts.IsEmpty
                            ? $"Client application '{ts.Name}' served at '{ts.RequestPath}' from '{projectPath}'."
                            : $"Client application '{ts.Name}' served at '{ts.RequestPath}' from '{projectPath}' (facts: {facts})." );
        }
        scope.Append( "} ) {}" ).NewLine();
        return WriteClientsProps( monitor, binPath.ProjectPath, clients.Count > 0 ? items.ToString() : null )
                ? CSCodeGenerationResult.Success
                : CSCodeGenerationResult.Failed;
    }

    // The "$StObjGen/Clients.props" of the host (when the BinPath has a host project): written when it changes, deleted
    // without client application.
    static bool WriteClientsProps( IActivityMonitor monitor, NormalizedPath hostProject, string? items )
    {
        if( hostProject.IsEmptyPath || !System.IO.Directory.Exists( hostProject ) ) return true;
        var file = hostProject.AppendPart( "$StObjGen" ).AppendPart( ClientsPropsFileName );
        try
        {
            if( items == null )
            {
                if( System.IO.File.Exists( file ) ) System.IO.File.Delete( file );
                return true;
            }
            var nl = Environment.NewLine;
            // The publication reads it when it runs (the MSBuild task on its target): the builder writes it during the build of
            // the host, after the evaluation of the host project.
            var text = $"<Project>{nl}  <!-- Generated by the TypeScript engine (the client applications of the host): do not edit. -->{nl}"
                       + $"  <ItemGroup>{nl}{items}  </ItemGroup>{nl}"
                       + $"  <Target Name=\"CKGetClientApplications\" Returns=\"@(CKClientApplication)\" />{nl}</Project>{nl}";
            if( System.IO.File.Exists( file ) && System.IO.File.ReadAllText( file ) == text ) return true;
            System.IO.Directory.CreateDirectory( file.RemoveLastPart() );
            System.IO.File.WriteAllText( file, text );
            return true;
        }
        catch( Exception ex )
        {
            monitor.Error( $"While writing '{file}'.", ex );
            return false;
        }
    }

    static string Escape( string s ) => System.Security.SecurityElement.Escape( s );

    // The yarn of a client application: the first ".yarn/releases/yarn*js" in or above its folder (the lookup of its dev
    // server).
    static NormalizedPath? FindYarn( NormalizedPath projectPath )
    {
        for( var d = projectPath; !d.IsEmptyPath; d = d.RemoveLastPart() )
        {
            var releases = d.AppendPart( ".yarn" ).AppendPart( "releases" );
            if( System.IO.Directory.Exists( releases ) )
            {
                var yarn = System.IO.Directory.GetFiles( releases )
                                              .FirstOrDefault( f => System.IO.Path.GetFileName( f ).StartsWith( "yarn" ) && f.EndsWith( "js" ) );
                if( yarn != null ) return new NormalizedPath( yarn );
            }
        }
        return null;
    }

    // The union of the facts of the selected packages of a client application.
    static ClientApplicationFacts GetFacts( TypeScriptPackageSelection selection, string configurationName )
    {
        return ClientApplicationFacts.Create( selection.GetPackages( configurationName )
                                                       .SelectMany( t => t.GetCustomAttributes<PartyClientFactAttribute>( inherit: false ) )
                                                       .Select( a => new System.Collections.Generic.KeyValuePair<string, string>( a.Key, a.Value ) ) );
    }
}
