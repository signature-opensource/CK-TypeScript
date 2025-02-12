using CK.Core;
using CK.TypeScript.LiveEngine;
using System;
using System.IO;
using System.Reflection;
using System.Threading;


var monitor = new ActivityMonitor();
monitor.Output.RegisterClient( new ColoredActivityMonitorConsoleClient() );

var pathContext = new LiveStatePathContext( Environment.CurrentDirectory );
var stateFilesFilter = new CKGenTransformFilter( pathContext );

Console.WriteLine( $"CK.TypeScript.LiveEngine v{CSemVer.InformationalVersion.ReadFromAssembly(Assembly.GetExecutingAssembly()).Version}" );
for(; ; )
{
    monitor.Info( $"""
    Waiting for file:
    -> {pathContext.PrimaryStateFile}
    """ );
    var liveState = LiveState.Load( monitor, pathContext );
    while( liveState == null )
    {
        Thread.Sleep( 500 );
        liveState = LiveState.Load( monitor, pathContext );
    }
    monitor.Info( "Starting watcher." );
    var runner = new Runner( liveState, stateFilesFilter );
    await runner.RunAsync( monitor );
}
