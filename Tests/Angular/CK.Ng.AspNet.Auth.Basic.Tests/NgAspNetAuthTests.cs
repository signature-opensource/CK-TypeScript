using CK.Core;
using CK.Testing;
using Microsoft.AspNetCore.Builder;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.Ng.AspNet.Auth.Basic.Tests;

[TestFixture]
public class NgAspNetAuthTests
{
    [Test]
    public async Task CK_Ng_AspNet_Auth_Basic_Async()
    {
        var targetProjectPath = TestHelper.GetTypeScriptInlineTargetProjectPath();

        var configuration = TestHelper.CreateDefaultEngineConfiguration();
        configuration.FirstBinPath.Path = TestHelper.BinFolder;
        configuration.FirstBinPath.Assemblies.Add( "CK.Ng.PublicPage" );
        configuration.FirstBinPath.Assemblies.Add( "CK.Ng.AspNet.Auth.Basic" );
        configuration.FirstBinPath.Types.Add( typeof( MyUserInfoBox.MyUserInfoBoxPackage ) );
        configuration.FirstBinPath.Types.Add( typeof( MyLayout.MyLayoutPackage ) );
        configuration.FirstBinPath.Types.Add( typeof( PublicChild.PublicChildComponent ) );
        configuration.RevertOrderingNames = true;

        var tsConfig = configuration.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath );
        tsConfig.ActiveCultures.Add( NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr" ) );
        var map = (await configuration.RunSuccessfullyAsync()).LoadMap();

        // Checks that NgZorro AppStyleImport works as expected.
        var srcStyles = File.ReadAllLines( targetProjectPath.Combine( "src/styles.less" ) );
        srcStyles.ShouldContain( "@import 'ng-zorro-antd/ng-zorro-antd.less';" )
                 .ShouldContain( "@import '../ck-gen/styles/styles.less';" );

        var builder = WebApplication.CreateSlimBuilder();

        await using var server = await builder.CreateRunningAspNetAuthenticationServerAsync( map, o => o.SlidingExpirationTime = TimeSpan.FromMinutes( 10 ) );
        await using var runner = TestHelper.CreateTypeScriptRunner( targetProjectPath, server.ServerAddress );
        await TestHelper.SuspendAsync( resume => resume );
        runner.Run();
    }
}
