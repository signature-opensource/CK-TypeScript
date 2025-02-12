using CK.Core;
using CK.TypeScript.LiveEngine;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

CancellationTokenSource ctrlCHandler = new CancellationTokenSource();
using var sigInt = PosixSignalRegistration.Create( PosixSignal.SIGINT, c => ctrlCHandler.Cancel() );

var monitor = new ActivityMonitor();
monitor.Output.RegisterClient( new ColoredActivityMonitorConsoleClient() );

var pathContext = new LiveStatePathContext( Environment.CurrentDirectory );
var stateFilesFilter = new CKGenTransformFilter( pathContext );

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

