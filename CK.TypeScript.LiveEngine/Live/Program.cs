using CK.Core;
using CK.TypeScript.LiveEngine;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading;

CancellationTokenSource ctrlCHandler = new CancellationTokenSource();
using var sigInt = PosixSignalRegistration.Create( PosixSignal.SIGINT, c => ctrlCHandler.Cancel() );

var monitor = new ActivityMonitor();
monitor.Output.RegisterClient( new ColoredActivityMonitorConsoleClient() );

var pathContext = new LiveStatePathContext( Environment.CurrentDirectory );
var stateFilesFilter = new CKGenTransformFilter( pathContext );

// Brutal but enough: by design versions are aligned. We don't need to bother
// with different versions. But this doesn't (shouldn't) handle native libraries.
// If native library support is required, this would need to be enhanced.
AssemblyLoadContext.Default.Resolving += static ( AssemblyLoadContext ctx, AssemblyName a ) =>
{
    return ctx.LoadFromAssemblyPath( System.IO.Path.Combine( AppContext.BaseDirectory, a.Name + ".dll" ) );
};

Console.WriteLine( $"CK.TypeScript.LiveEngine v{CSemVer.InformationalVersion.ReadFromAssembly( Assembly.GetExecutingAssembly() ).Version}" );

while( !ctrlCHandler.IsCancellationRequested )
{
    monitor.Info( $"""
                    Waiting for file:
                    -> {pathContext.PrimaryStateFile}
                    """ );
    var liveState = await LiveState.WaitForStateAsync( monitor, pathContext, ctrlCHandler.Token );
    monitor.Info( "Running watcher." );
    var runner = new Runner( liveState, stateFilesFilter );
    await runner.RunAsync( monitor, ctrlCHandler.Token );
    monitor.Info( "Watcher stopped." );
}

