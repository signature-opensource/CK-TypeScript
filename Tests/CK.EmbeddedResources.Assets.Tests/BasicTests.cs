using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System;
using static CK.Testing.MonitorTestHelper;

namespace CK.EmbeddedResources.Assets.Tests;

[TestFixture]
public class BasicTests
{
    [Test]
    public void TargetPath_can_be_empty()
    {
        var resources = typeof( BasicTests ).Assembly.GetResources();

        var c = resources.CreateResourcesContainerForType( TestHelper.Monitor, typeof( T1.Package ) );
        c.IsValid.Should().BeTrue();
        c.LoadAssets( TestHelper.Monitor, "some/path", out var assets, "assets" ).Should().BeTrue();
        Throw.Assert( assets != null );
        assets.Assets.Count.Should().Be( 1 );
        assets.Assets["favicon.ico"].Override.Should().Be( ResourceOverrideKind.None );
        assets.Assets["favicon.ico"].Origin.LocalFilePath.Should().NotBeNullOrWhiteSpace();
        assets.Assets["favicon.ico"].Origin.ResourceName.Should().Be( "ck@T1/Res/assets/favicon.ico" );
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
