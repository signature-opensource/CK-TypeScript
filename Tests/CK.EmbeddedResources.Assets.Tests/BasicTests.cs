using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.IO;
using static CK.Testing.MonitorTestHelper;

namespace CK.EmbeddedResources.Assets.Tests;

[TestFixture]
public class BasicTests
{
    [Test]
    public void TargetPath_can_be_empty()
    {
        Type type = typeof( T1.Package );
        // This is a Local project: we obtain a FileSystemResourceContainer.
        string localFavIconPath;
        {
            var c = type.CreateResourcesContainer( TestHelper.Monitor );
            c.IsValid.Should().BeTrue();
            c.Should().BeAssignableTo<FileSystemResourceContainer>();
            c.LoadAssets( TestHelper.Monitor, "some/path", out var assets, "assets" ).Should().BeTrue();
            Throw.Assert( assets != null );
            assets.Assets.Count.Should().Be( 1 );
            // assets.jsonc defines is { "targetPath": "" }
            // The defaultTargetPath "some/path" is ignored.
            assets.Assets["favicon.ico"].Override.Should().Be( ResourceOverrideKind.None );
            // The ResourceName is the full path of the file.
            var sep = Path.DirectorySeparatorChar;
            localFavIconPath = assets.Assets["favicon.ico"].Origin.ResourceName;
            localFavIconPath.Should().Be( $"{c.ResourcePrefix}assets{sep}favicon.ico" );
            localFavIconPath.Should().BeSameAs( assets.Assets["favicon.ico"].Origin.LocalFilePath );
        }
        // Same as above but from the embedded resources.
        {
            var c = type.Assembly.GetResources().CreateResourcesContainerForType( TestHelper.Monitor, type );
            c.IsValid.Should().BeTrue();
            c.Should().BeAssignableTo<AssemblyResourceContainer>();
            c.LoadAssets( TestHelper.Monitor, "some/path", out var assets, "assets" ).Should().BeTrue();
            Throw.Assert( assets != null );
            assets.Assets.Count.Should().Be( 1 );
            // assets.jsonc defines is { "targetPath": "" }
            // The defaultTargetPath "some/path" is ignored.
            assets.Assets["favicon.ico"].Override.Should().Be( ResourceOverrideKind.None );
            var o = assets.Assets["favicon.ico"].Origin;
            // The LocalFilePath exists.
            o.LocalFilePath.Should().Be( localFavIconPath );
            c.ResourcePrefix.Should().Be( "ck@T1/Res/" );
            o.ResourceName.Should().Be( $"{c.ResourcePrefix}assets/favicon.ico" );
        }
    }

    [Test]
    public void Override_required_failure()
    {
        using( TestHelper.Monitor.CollectTexts( out var logs  ) )
        {
            var f = TryLoadFinal( TestHelper.Monitor, typeof( T1.Package ), typeof( T2.Package ) );
            logs.Should().Contain( """
                    Asset 'favicon.ico' in resources of 'CK.EmbeddedResources.Assets.Tests.T2.Package' type overides the existing asset from resources of 'CK.EmbeddedResources.Assets.Tests.T1.Package' type.
                    An explicit override declaration "O": [... "favicon.ico" ...] is required.
                    """ );
        }
    }

    [Test]
    public void Override_required_success()
    {
        var f = TryLoadFinal( TestHelper.Monitor, typeof( T1.Package ), typeof( T3.Package ) );
        Throw.Assert( f != null );
        f.Final.Assets["favicon.ico"].Origin.Container.DisplayName.Should().Be( "resources of 'CK.EmbeddedResources.Assets.Tests.T3.Package' type" );
    }

    static FinalResourceAssetSet? TryLoadFinal( IActivityMonitor monitor, params Type[] types )
    {
        var resources = typeof( BasicTests ).Assembly.GetResources();
        var f = new FinalResourceAssetSet( true );
        foreach( var type in types )
        {
            Throw.Assert( type.Namespace != null );
            var c = resources.CreateResourcesContainerForType( TestHelper.Monitor, type );
            c.IsValid.Should().BeTrue();
            if( !c.LoadAssets( monitor, type.Namespace.Replace( '.', '/' ), out var assets, "assets" ) )
            {
                return null;
            }
            Throw.Assert( assets != null );
            if( !f.Add( TestHelper.Monitor, assets ) )
            {
                return null;
            }
        }
        return f;
    }
}
