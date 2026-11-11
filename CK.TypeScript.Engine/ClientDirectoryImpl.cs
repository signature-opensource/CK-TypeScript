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
/// </summary>
public sealed class ClientDirectoryImpl : CSCodeGeneratorType
{
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
                 .Append( ts.UseDist ? "true" : "false" ).Append( ", " )
                 .Append( "[" );
            bool first = true;
            foreach( var (key, value) in facts.Pairs )
            {
                if( !first ) scope.Append( ", " );
                first = false;
                scope.Append( "(" ).AppendSourceString( key ).Append( ", " ).AppendSourceString( value ).Append( ")" );
            }
            scope.Append( "] )," ).NewLine();
            monitor.Info( facts.IsEmpty
                            ? $"Client application '{ts.Name}' served at '{ts.RequestPath}' from '{projectPath}'."
                            : $"Client application '{ts.Name}' served at '{ts.RequestPath}' from '{projectPath}' (facts: {facts})." );
        }
        scope.Append( "} ) {}" ).NewLine();

        // Temporary, waiting for the Unified Configuration: the root party of a host declares its client applications.
        // Each BinPath of the group can be a different host.
        bool success = true;
        foreach( var b in c.CurrentRun.ConfigurationGroup.SimilarConfigurations )
        {
            var names = b.FindAspect<TypeScriptBinPathAspectConfiguration>()?.AllConfigurations
                         .Where( ts => ts.RequestPath != null )
                         .Select( ts => ts.Name! ) // A client application has a name.
                         .ToList()
                        ?? [];
            success &= HostClientDeclarations.EnsureDeclared( monitor, b.ProjectPath, names );
        }
        return success ? CSCodeGenerationResult.Success : CSCodeGenerationResult.Failed;
    }

    // The union of the facts of the selected packages of a client application.
    static ClientApplicationFacts GetFacts( TypeScriptPackageSelection selection, string configurationName )
    {
        return ClientApplicationFacts.Create( selection.GetPackages( configurationName )
                                                       .SelectMany( t => t.GetCustomAttributes<PartyClientFactAttribute>( inherit: false ) )
                                                       .Select( a => new System.Collections.Generic.KeyValuePair<string, string>( a.Key, a.Value ) ) );
    }
}
