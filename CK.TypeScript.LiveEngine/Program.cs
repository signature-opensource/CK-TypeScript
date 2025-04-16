using CK.Core;
using CK.TypeScript.LiveEngine;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading;

var (logFilter, debounce, launchDebugger) = DisplayHeaderAndHandleArguments( args );

// Don't try to attach a debugger if one is already attached.
if( launchDebugger && !Debugger.IsAttached )
{
    Debugger.Launch();
}

var monitor = new ActivityMonitor();
monitor.MinimalFilter = logFilter;
monitor.Output.RegisterClient( new ColoredActivityMonitorConsoleClient() );

CancellationTokenSource ctrlCHandler = new CancellationTokenSource();
using var sigInt = PosixSignalRegistration.Create( PosixSignal.SIGINT, c => ctrlCHandler.Cancel() );

// Brutal but enough: by design versions are aligned. We don't need to bother
// with different versions. But this doesn't (shouldn't) handle native libraries.
// If native library support is required, this would need to be enhanced.
AssemblyLoadContext.Default.Resolving += static ( AssemblyLoadContext ctx, AssemblyName a ) =>
{
    return ctx.LoadFromAssemblyPath( Path.Combine( AppContext.BaseDirectory, a.Name + ".dll" ) );
};

var ckGenAppPath = Path.Combine( Environment.CurrentDirectory, "ck-gen-app" ) + Path.DirectorySeparatorChar;
var liveStateFilePath = ckGenAppPath + ResSpace.LiveStateFileName;

await Runner.RunAsync( monitor, liveStateFilePath, debounce, ctrlCHandler.Token );

static (LogFilter LogFilter, uint Debounce, bool LaunchDebugger) DisplayHeaderAndHandleArguments( string[] args )
{
    Console.WriteLine( $"CK.TypeScript.LiveEngine v{CSemVer.InformationalVersion.ReadFromAssembly( Assembly.GetExecutingAssembly() ).Version}" );
    if( Array.IndexOf( args, "-h" ) >= 0
        || Array.IndexOf( args, "-?" ) >= 0
        || Array.IndexOf( args, "--help" ) >= 0 )
    {
        Console.WriteLine( """
        Usage:
            -?, -h, -help:   Display help.

            -v, --vervosity: Set log verbosity
                                 Q[uiet]       Error groups and line only.
                                 M[inimal]     Information groups and Warning lines.
                                 N[ormal]      Trace groups and Warning lines.
                                 D[etailed]    Trace groups and lines.
                                 Diag[nostic]  Debug groups and lines.

            --debug-launch:   Launch a debugger when starting.

            -d, --debounce:   Debounce time in milliseconds for file change events.
                              Default to 100ms.
                              Provided value is clamped between 40 and 1000.
        """ );
    }

    LogFilter log = LogFilter.Normal;
    string logName = nameof( LogFilter.Normal );
    int idxV = Array.IndexOf( args, "--verbosity" );
    if( idxV < 0 ) idxV = Array.IndexOf( args, "-v" );
    if( idxV >= 0 )
    {
        if( ++idxV == args.Length )
        {
            Console.WriteLine( "Missing verbosity level." );
            log = LogFilter.Diagnostic;
            logName = nameof( LogFilter.Diagnostic );
        }
        else
        {
            var s = args[idxV].ToUpperInvariant();
            switch( s[0] )
            {
                case 'Q':
                    log = LogFilter.Quiet;
                    logName = nameof( LogFilter.Quiet );
                    break;
                case 'M':
                    log = LogFilter.Minimal;
                    logName = nameof( LogFilter.Minimal );
                    break;
                case 'N':
                    log = LogFilter.Normal;
                    logName = nameof( LogFilter.Normal );
                    break;
                case 'D':
                    if( s.Length > 1 && s[1] == 'I' )
                    {
                        log = LogFilter.Diagnostic;
                        logName = nameof( LogFilter.Diagnostic );
                    }
                    else
                    {
                        log = LogFilter.Detailed;
                        logName = nameof( LogFilter.Detailed );
                    }
                    break;
            }
        }
    }
    Console.WriteLine( $"Using --verbosity {logName}" );

    uint debounce = 100;
    int idxD = Array.IndexOf( args, "--debounce" );
    if( idxD < 0 ) idxD = Array.IndexOf( args, "-d" );
    if( idxD >= 0 )
    {
        if( ++idxD == args.Length )
        {
            Console.WriteLine( "Missing debounce time." );
        }
        else
        {
            if( !uint.TryParse( args[idxV], out var dValue ) )
            {
                Console.WriteLine( "Invalid debounce time." );
            }
            else
            {
                debounce = uint.Clamp( dValue, 40, 1000 );
            }
        }
    }
    Console.WriteLine( $"Using --debounce {debounce}" );

    return (log, debounce, Array.IndexOf( args, "--debug-launch" ) >= 0);
}

