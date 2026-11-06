using CK.Core;
using CK.EmbeddedResources;
using CK.Html.Transform;
using CK.Transform.Core;
using CK.TypeScript.LiveEngine;
using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.TypeScript.Tests;

/// <summary>
/// The Live side of the <see cref="TransformableFileHandler"/>: the local files change while the
/// <see cref="Runner"/> watches them, and the installed files follow.
/// </summary>
[TestFixture]
public class TransformableLiveTests
{
    [Test]
    public async Task items_transformers_and_folders_changes_Async( CancellationToken testCancellation )
    {
        Assume.That( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ), "FileSystemWatcher on Unix has to be tested." );

        var root = TestHelper.CleanupFolder( TestHelper.TestProjectFolder.AppendPart( "TransformableLive" ), ensureFolderAvailable: true );
        File.WriteAllText( root.AppendPart( ".gitignore" ), "*" );
        var pathA = root.AppendPart( "A" );
        var pathB = root.AppendPart( "B" );
        var target = root.AppendPart( "Target" );

        // A has an item, B (that requires A) transforms it.
        Write( pathA.Combine( "Comp/hello.html" ), "<p>Hello</p>" );
        Write( pathB.AppendPart( "hello.t" ), Transformer( "<b>B</b>" ) );

        var space = Install( root, target, "A", "B" );
        var installed = target.Combine( "A/Comp/hello.html" );
        File.ReadAllText( installed ).ShouldBe( "<b>B</b><p>Hello</p>" );

        var cts = new CancellationTokenSource();
        using var fromTest = testCancellation.UnsafeRegister( cts => ((CancellationTokenSource)cts!).Cancel(), cts );
        var running = Runner.RunAsync( new ActivityMonitor( "Live monitor." ),
                                       space.Data.LiveStatePath + ResSpace.LiveStateFileName,
                                       100,
                                       cts.Token );
        try
        {
            // Lets the runner read the Live state and start watching.
            await Task.Delay( 500, testCancellation );

            using( TestHelper.Monitor.OpenInfo( "An item changes." ) )
            {
                Write( pathA.Combine( "Comp/hello.html" ), "<p>Hello2</p>" );
                await WaitForAsync( installed, "<b>B</b><p>Hello2</p>" );
            }
            using( TestHelper.Monitor.OpenInfo( "A transformer changes." ) )
            {
                Write( pathB.AppendPart( "hello.t" ), Transformer( "<i>B2</i>" ) );
                await WaitForAsync( installed, "<i>B2</i><p>Hello2</p>" );
            }
            using( TestHelper.Monitor.OpenInfo( "A folder is renamed." ) )
            {
                Directory.Move( pathA.AppendPart( "Comp" ), pathA.AppendPart( "Comp2" ) );
                var moved = target.Combine( "A/Comp2/hello.html" );
                await WaitForAsync( moved, "<i>B2</i><p>Hello2</p>" );
                await WaitForAsync( installed, null );
            }
            using( TestHelper.Monitor.OpenInfo( "The package's folder is renamed away, and back." ) )
            {
                var moved = target.Combine( "A/Comp2/hello.html" );
                var away = root.AppendPart( "A-away" );
                Directory.Move( pathA, away );
                await WaitForAsync( moved, null );
                Directory.Move( away, pathA );
                await WaitForAsync( moved, "<i>B2</i><p>Hello2</p>" );
            }
        }
        finally
        {
#pragma warning disable VSTHRD103 // Call async methods when in an async method
            cts.Cancel( throwOnFirstException: true );
#pragma warning restore VSTHRD103
            await running.WaitAsync( testCancellation );
        }
    }

    // The generic transformer language (<t>), and the auto language of the transformers of html transformers (<html.t>).
    [TestCase( "t", "more.t" )]
    [TestCase( "html.t", "more.html.t.t" )]
    public async Task transformers_of_transformers_changes_Async( string language, string fileName, CancellationToken testCancellation )
    {
        Assume.That( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ), "FileSystemWatcher on Unix has to be tested." );

        var root = TestHelper.CleanupFolder( TestHelper.TestProjectFolder.AppendPart( $"TransformableLiveTT-{language}" ), ensureFolderAvailable: true );
        File.WriteAllText( root.AppendPart( ".gitignore" ), "*" );
        var target = root.AppendPart( "Target" );

        // A has an item, B transforms it with a named function that has an injection point,
        // C transforms B's function: it injects a statement.
        Write( root.Combine( "A/hello.html" ), "<p>Hello</p>" );
        Write( root.Combine( "B/hello.t" ), NamedTransformer( "<b>B</b>" ) );
        var more = root.Combine( "C" ).AppendPart( fileName );
        Write( more, TransformerOfTransformer( "<u>C</u>", language: language ) );

        var space = Install( root, target, "A", "B", "C" );
        var installed = target.Combine( "A/hello.html" );
        File.ReadAllText( installed ).ShouldBe( "<b>B</b><u>C</u><p>Hello</p>" );

        var cts = new CancellationTokenSource();
        using var fromTest = testCancellation.UnsafeRegister( cts => ((CancellationTokenSource)cts!).Cancel(), cts );
        var running = Runner.RunAsync( new ActivityMonitor( "Live monitor." ),
                                       space.Data.LiveStatePath + ResSpace.LiveStateFileName,
                                       100,
                                       cts.Token );
        try
        {
            await Task.Delay( 500, testCancellation );

            using( TestHelper.Monitor.OpenInfo( "The transformer of transformer changes." ) )
            {
                Write( more, TransformerOfTransformer( "<i>C2</i>", language: language ) );
                await WaitForAsync( installed, "<b>B</b><i>C2</i><p>Hello</p>" );
            }
            using( TestHelper.Monitor.OpenInfo( "The transformed transformer changes." ) )
            {
                Write( root.Combine( "B/hello.t" ), NamedTransformer( "<b>B2</b>" ) );
                await WaitForAsync( installed, "<b>B2</b><i>C2</i><p>Hello</p>" );
            }
            using( TestHelper.Monitor.OpenInfo( "The transformer of transformer disappears, and comes back." ) )
            {
                File.Delete( more );
                await WaitForAsync( installed, "<b>B2</b><p>Hello</p>" );
                Write( more, TransformerOfTransformer( "<u>C3</u>", language: language ) );
                await WaitForAsync( installed, "<b>B2</b><u>C3</u><p>Hello</p>" );
            }
        }
        finally
        {
#pragma warning disable VSTHRD103 // Call async methods when in an async method
            cts.Cancel( throwOnFirstException: true );
#pragma warning restore VSTHRD103
            await running.WaitAsync( testCancellation );
        }
    }

    [Test]
    public async Task an_unnamed_function_rebound_to_a_moved_item_is_renamed_Async( CancellationToken testCancellation )
    {
        Assume.That( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ), "FileSystemWatcher on Unix has to be tested." );

        var root = TestHelper.CleanupFolder( TestHelper.TestProjectFolder.AppendPart( "TransformableLiveRename" ), ensureFolderAvailable: true );
        File.WriteAllText( root.AppendPart( ".gitignore" ), "*" );
        var target = root.AppendPart( "Target" );

        // B's function has no name: its name is computed from its target, "B(A/Comp/hello.html)".
        Write( root.Combine( "A/Comp/hello.html" ), "<p>Hello</p>" );
        Write( root.Combine( "B/hello.t" ), UnnamedTransformerWithInjectionPoint( "<b>B</b>" ) );
        Directory.CreateDirectory( root.AppendPart( "C" ) );

        var space = Install( root, target, "A", "B", "C" );
        File.ReadAllText( target.Combine( "A/Comp/hello.html" ) ).ShouldBe( "<b>B</b><p>Hello</p>" );

        var cts = new CancellationTokenSource();
        using var fromTest = testCancellation.UnsafeRegister( cts => ((CancellationTokenSource)cts!).Cancel(), cts );
        var running = Runner.RunAsync( new ActivityMonitor( "Live monitor." ),
                                       space.Data.LiveStatePath + ResSpace.LiveStateFileName,
                                       100,
                                       cts.Token );
        try
        {
            await Task.Delay( 500, testCancellation );

            Directory.Move( root.Combine( "A/Comp" ), root.Combine( "A/Comp2" ) );
            var moved = target.Combine( "A/Comp2/hello.html" );
            await WaitForAsync( moved, "<b>B</b><p>Hello</p>" );

            // The rebound function is now "B(A/Comp2/hello.html)": a new transformer of transformer finds it by this name.
            Write( root.Combine( "C/more.t" ), TransformerOfTransformer( "<u>C</u>", "B(A/Comp2/hello.html)" ) );
            await WaitForAsync( moved, "<b>B</b><u>C</u><p>Hello</p>" );
        }
        finally
        {
#pragma warning disable VSTHRD103 // Call async methods when in an async method
            cts.Cancel( throwOnFirstException: true );
#pragma warning restore VSTHRD103
            await running.WaitAsync( testCancellation );
        }
    }

    static string UnnamedTransformerWithInjectionPoint( string inserted ) => $$""""
        create <html> transformer
        begin
            insert """{{inserted}}""" before "<p>";
            //<More/>
        end
        """";

    static string NamedTransformer( string inserted ) => $$""""
        create <html> transformer BHello as
        begin
            insert """{{inserted}}""" before "<p>";
            //<More/>
        end
        """";

    static string TransformerOfTransformer( string inserted, string targetFunction = "BHello", string language = "t" ) => $$"""""
        create <{{language}}> transformer on "{{targetFunction}}"
        begin
            inject """"
                   insert """{{inserted}}""" before "<p>";
                   """" into <More>;
        end
        """"";

    static string Transformer( string inserted ) => $$""""
        create <html> transformer
        begin
            insert """{{inserted}}""" before "<p>";
        end
        """";

    static void Write( NormalizedPath path, string text )
    {
        Directory.CreateDirectory( path.RemoveLastPart() );
        File.WriteAllText( path, text );
    }

    // Polls the file until it has the expected content (null: until it doesn't exist).
    // The runner is asynchronous (FileSystemWatcher + debounce): a fixed delay is not enough under load.
    static async Task WaitForAsync( NormalizedPath path, string? expected )
    {
        const int pollMs = 50;
        const int timeoutMs = 5000;
        string? text = null;
        for( int elapsed = 0; elapsed < timeoutMs; elapsed += pollMs )
        {
            await Task.Delay( pollMs );
            try
            {
                text = File.Exists( path ) ? File.ReadAllText( path ) : null;
                if( text == expected ) return;
            }
            catch( IOException )
            {
                // The file is being written: retry.
            }
        }
        text.ShouldBe( expected, $"File '{path}' after {timeoutMs} ms." );
    }

    // Registers the local packages (in the root/<name> folders): each one requires the previous one.
    static ResSpace Install( NormalizedPath root, NormalizedPath target, params string[] names )
    {
        var config = new ResSpaceConfiguration();
        config.AppResourcesLocalPath = root.AppendPart( "App" );
        Directory.CreateDirectory( config.AppResourcesLocalPath );
        ResPackageDescriptor? previous = null;
        foreach( var name in names )
        {
            var d = config.RegisterPackage( TestHelper.Monitor, name, name,
                                            new FileSystemResourceContainer( root.AppendPart( name ) + Path.DirectorySeparatorChar, name ),
                                            new FileSystemResourceContainer( root.AppendPart( name + "[After]" ) + Path.DirectorySeparatorChar, name + "[After]" ),
                                            isOptional: false ).ShouldNotBeNull();
            if( previous != null ) d.Requires.Add( previous );
            previous = d;
        }

        var collector = config.Build( TestHelper.Monitor ).ShouldNotBeNull();
        var spaceData = new ResSpaceDataBuilder( collector ).Build( TestHelper.Monitor ).ShouldNotBeNull();
        var builder = new ResSpaceBuilder( spaceData );
        var handler = new TransformableFileHandler( new InitialFileSystemInstaller( target ),
                                                    new TransformerHost( new HtmlLanguage() ),
                                                    externalItemResolver: null );
        builder.RegisterHandler( TestHelper.Monitor, handler ).ShouldBeTrue();
        var space = builder.Build( TestHelper.Monitor ).ShouldNotBeNull();
        space.Install( TestHelper.Monitor ).ShouldBeTrue();
        return space;
    }
}
