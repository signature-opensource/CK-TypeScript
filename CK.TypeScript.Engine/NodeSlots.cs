using CK.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace CK.Setup;

/// <summary>
/// Limits the number of node processes (yarn, Angular builds, Jest) that run at the same time on the machine, whatever
/// the processes that launch them: parallel test runs and builds start many of them, and each one can take more than a
/// gigabyte. Too many of them exhaust the memory of the machine.
/// <para>
/// A slot is one of N files of a machine-wide folder, held open exclusively while one node process runs: the system
/// releases it when the process that holds it ends, even when it crashes. A slot is held only while its process runs, it
/// is never held while waiting for anything else (a lock, another slot): the slots can't deadlock.
/// </para>
/// <para>
/// N is the "CK_NODE_MAX_CONCURRENCY" environment variable when it is a positive integer, otherwise
/// <c>Max( 1, Min( 4, ProcessorCount / 4 ) )</c>.
/// </para>
/// </summary>
static class NodeSlots
{
    /// <summary>
    /// The name of the environment variable that sets the maximal number of node processes.
    /// </summary>
    internal const string MaxConcurrencyVariable = "CK_NODE_MAX_CONCURRENCY";

    static readonly TimeSpan _logPeriod = TimeSpan.FromSeconds( 30 );

    /// <summary>
    /// Gets the maximal number of node processes that run at the same time.
    /// </summary>
    internal static int MaxConcurrency
    {
        get
        {
            var v = Environment.GetEnvironmentVariable( MaxConcurrencyVariable );
            return int.TryParse( v, out var n ) && n > 0 ? n : Math.Max( 1, Math.Min( 4, Environment.ProcessorCount / 4 ) );
        }
    }

    /// <summary>
    /// Gets the machine-wide folder of the slots.
    /// </summary>
    internal static string DefaultFolder => Path.Combine( Path.GetTempPath(), "CK-Node-Slots" );

    /// <summary>
    /// Acquires a slot: waits until one of the <see cref="MaxConcurrency"/> slots is free.
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    /// <returns>The slot to dispose once the node process has ended.</returns>
    internal static IDisposable Acquire( IParallelLogger logger ) => Acquire( logger, DefaultFolder, MaxConcurrency );

    /// <summary>
    /// Acquires a slot in a folder, among a number of slots.
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    /// <param name="folder">The folder of the slots.</param>
    /// <param name="maxConcurrency">The number of slots.</param>
    /// <returns>The slot to dispose once the node process has ended.</returns>
    internal static IDisposable Acquire( IParallelLogger logger, string folder, int maxConcurrency )
    {
        Throw.CheckArgument( maxConcurrency > 0 );
        Directory.CreateDirectory( folder );
        Stopwatch? waiting = null;
        TimeSpan nextLog = TimeSpan.Zero;
        for(; ; )
        {
            // Starting at a random slot spreads the attempts of the processes.
            int start = Random.Shared.Next( maxConcurrency );
            for( int i = 0; i < maxConcurrency; ++i )
            {
                var path = Path.Combine( folder, $"slot-{(start + i) % maxConcurrency}.lock" );
                try
                {
                    var slot = new FileStream( path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.None );
                    if( waiting != null ) logger.Info( $"Node slot acquired after {waiting.Elapsed.TotalSeconds:N0} seconds." );
                    return slot;
                }
                catch( IOException )
                {
                    // The slot is held by another node process.
                }
                catch( UnauthorizedAccessException )
                {
                    // On Windows, a file being deleted or held with another access can also refuse the open.
                }
            }
            waiting ??= Stopwatch.StartNew();
            if( waiting.Elapsed >= nextLog )
            {
                logger.Info( $"Waiting for a node slot: the {maxConcurrency} slots of '{folder}' are used ({MaxConcurrencyVariable} sets their number)." );
                nextLog = waiting.Elapsed + _logPeriod;
            }
            // A random wait lowers the races between the waiting processes.
            Thread.Sleep( Random.Shared.Next( 200, 1000 ) );
        }
    }
}
