using CK.CodeGen;
using CK.Core;
using System;
using System.Linq;

namespace CK.Setup;

/// <summary>
/// Code generator of the <see cref="CK.TypeScript.ClientDirectory"/> service: its client applications are the TypeScript
/// configurations of the BinPath that are client applications (<see cref="TypeScriptBinPathAspectConfiguration.Kind"/>).
/// </summary>
public sealed class ClientDirectoryImpl : CSCodeGeneratorType
{
    /// <inheritdoc />
    public override CSCodeGenerationResult Implement( IActivityMonitor monitor, Type classType, ICSCodeGenerationContext c, ITypeScope scope )
    {
        // Skips the purely unified BinPath.
        if( c.CurrentRun.ConfigurationGroup.IsUnifiedPure ) return CSCodeGenerationResult.Success;

        var binPath = c.CurrentRun.ConfigurationGroup.Configuration;
        // The default client application is the last one: the host maps it after the other ones.
        var clients = binPath.FindAspect<TypeScriptBinPathAspectConfiguration>()?.AllConfigurations
                             .Where( ts => ts.RequestPath != null )
                             .OrderBy( ts => ts.Kind == ClientKind.DefaultApp )
                             .ToList()
                      ?? [];
        scope.Append( "public " ).Append( scope.Name ).Append( "() : base( new CK.TypeScript.ClientInfo[] {" ).NewLine();
        foreach( var ts in clients )
        {
            var projectPath = ts.TargetProjectPath.StartsWith( binPath.ProjectPath )
                                ? ts.TargetProjectPath.RemovePrefix( binPath.ProjectPath )
                                : ts.TargetProjectPath;
            var outputPath = new NormalizedPath( "dist" ).AppendPart( ts.TargetProjectPath.LastPart ).AppendPart( "browser" );
            scope.Append( "new CK.TypeScript.ClientInfo( " )
                 .AppendSourceString( ts.Name ).Append( ", " )
                 .AppendSourceString( ts.RequestPath ).Append( ", " )
                 .AppendSourceString( projectPath.Path ).Append( ", " )
                 .AppendSourceString( outputPath.Path ).Append( ", " )
                 .AppendSourceString( ts.TypeFilterName ).Append( ", " )
                 .Append( ts.DevServerPort!.Value ).Append( ", " )
                 .Append( ts.UseDist ? "true" : "false" ).Append( " )," ).NewLine();
            monitor.Info( $"Client application '{ts.Name}' served at '{ts.RequestPath}' from '{projectPath}'." );
        }
        scope.Append( "} ) {}" ).NewLine();
        return CSCodeGenerationResult.Success;
    }
}
