using CK.Core;
using CK.TypeScript.LiveEngine;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;


var monitor = new ActivityMonitor();
monitor.Output.RegisterClient( new ColoredActivityMonitorConsoleClient() );

NormalizedPath targetProfectPath = Environment.CurrentDirectory;

var liveState = LiveState.Load( monitor, targetProfectPath );
while( liveState == null )
{
    monitor.Info( "Waiting for LiveState..." );
    Thread.Sleep( 500 );
    liveState = LiveState.Load( monitor, targetProfectPath );
}


