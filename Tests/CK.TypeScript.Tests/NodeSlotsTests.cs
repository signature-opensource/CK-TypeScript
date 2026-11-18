using CK.Setup;
using NUnit.Framework;
using Shouldly;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.TypeScript.Tests;

[TestFixture]
public class NodeSlotsTests
{
    // A folder of slots of its own: the machine-wide one is shared with the other processes.
    static string CreateFolder()
    {
        var folder = Path.Combine( Path.GetTempPath(), "CK-Node-Slots-Tests", Guid.NewGuid().ToString( "N" ) );
        Directory.CreateDirectory( folder );
        return folder;
    }

    [Test]
    public async Task the_slots_limit_the_concurrency_Async()
    {
        var folder = CreateFolder();
        int current = 0;
        int max = 0;
        var runs = Enumerable.Range( 0, 6 ).Select( _ => Task.Run( () =>
        {
            using( NodeSlots.Acquire( TestHelper.Monitor.ParallelLogger, folder, 2 ) )
            {
                int c = Interlocked.Increment( ref current );
                int m;
                while( (m = Volatile.Read( ref max )) < c && Interlocked.CompareExchange( ref max, c, m ) != m ) ;
                Thread.Sleep( 300 );
                Interlocked.Decrement( ref current );
            }
        } ) ).ToArray();
        await Task.WhenAll( runs );
        max.ShouldBe( 2, "Never more than the 2 slots, and they are used." );
        Directory.Delete( folder, true );
    }

    [Test]
    public async Task a_slot_is_released_when_its_holder_ends_Async()
    {
        var folder = CreateFolder();
        var held = NodeSlots.Acquire( TestHelper.Monitor.ParallelLogger, folder, 1 );
        var waiting = Task.Run( () => NodeSlots.Acquire( TestHelper.Monitor.ParallelLogger, folder, 1 ) );
        await Task.Delay( 1500 );
        waiting.IsCompleted.ShouldBeFalse( "The only slot is held." );
        held.Dispose();
        (await waiting.WaitAsync( TimeSpan.FromSeconds( 10 ) )).Dispose();
        Directory.Delete( folder, true );
    }

    [Test]
    [Platform( "Win" )]
    public async Task a_slot_held_by_a_killed_process_is_released_Async()
    {
        var folder = CreateFolder();
        var slot = Path.Combine( folder, "slot-0.lock" );
        // Another process holds the only slot (as a node process run by another test process would)...
        using var holder = Process.Start( new ProcessStartInfo( "powershell",
                                          $"-NoProfile -Command \"while( -not $f ) {{ try {{ $f = [IO.File]::Open( '{slot}', 'OpenOrCreate', 'ReadWrite', 'None' ) }} catch {{ Start-Sleep -Milliseconds 50 }} }}; Start-Sleep 60\"" )
        {
            UseShellExecute = false,
            CreateNoWindow = true
        } )!;
        for( int i = 0; i < 100 && !IsHeld( slot ); ++i ) await Task.Delay( 100 );
        IsHeld( slot ).ShouldBeTrue( "The other process holds the slot." );
        var waiting = Task.Run( () => NodeSlots.Acquire( TestHelper.Monitor.ParallelLogger, folder, 1 ) );
        await Task.Delay( 1000 );
        waiting.IsCompleted.ShouldBeFalse();
        // ...and crashes: the system releases its slot.
        holder.Kill( entireProcessTree: true );
        (await waiting.WaitAsync( TimeSpan.FromSeconds( 10 ) )).Dispose();
        Directory.Delete( folder, true );

        static bool IsHeld( string path )
        {
            try
            {
                if( !File.Exists( path ) ) return false;
                using var f = new FileStream( path, FileMode.Open, FileAccess.ReadWrite, FileShare.None );
                return false;
            }
            catch( IOException )
            {
                return true;
            }
        }
    }

    [Test]
    public void MaxConcurrency_is_read_from_the_environment()
    {
        try
        {
            Environment.SetEnvironmentVariable( NodeSlots.MaxConcurrencyVariable, "7" );
            NodeSlots.MaxConcurrency.ShouldBe( 7 );
            Environment.SetEnvironmentVariable( NodeSlots.MaxConcurrencyVariable, "zero" );
            NodeSlots.MaxConcurrency.ShouldBe( Math.Max( 1, Math.Min( 4, Environment.ProcessorCount / 4 ) ) );
        }
        finally
        {
            Environment.SetEnvironmentVariable( NodeSlots.MaxConcurrencyVariable, null );
        }
    }
}
