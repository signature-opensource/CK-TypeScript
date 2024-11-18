using CK.Core;
using CK.Demo;
using CK.Ng.PublicSection;
using CK.Ng.AspNet.Auth;
using CK.Testing;
using NUnit.Framework;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.TS.Angular.Tests;

[TestFixture]
public class AngularTests
{
    [Test]
    public async Task CK_TS_Angular_Async()
    {
        var targetProjectPath = TestHelper.GetTypeScriptInlineTargetProjectPath();

        var configuration = TestHelper.CreateDefaultEngineConfiguration();
        var ts = configuration.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath );
        ts.ActiveCultures.Add( NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr" ) );
        ts.ActiveCultures.Add( NormalizedCultureInfo.EnsureNormalizedCultureInfo( "en-GB" ) );
        ts.ActiveCultures.Add( NormalizedCultureInfo.EnsureNormalizedCultureInfo( "en-US" ) );


        ts.CKGenBuildMode = false;

        configuration.FirstBinPath.Assemblies.Add( "CK.TS.Angular" );
        configuration.FirstBinPath.Types.Add( typeof( DemoNgModule ),
                                              // CK.Ng.AspNet.Auth folder.
                                              typeof( LoginComponent ),
                                              typeof( PasswordLostComponent ),
                                              typeof( SomeAuthPackage ),
                                              // CK.Ng.PublicSection
                                              typeof( PublicSectionPackage ),
                                              typeof( PublicFooterComponent ),
                                              typeof( PublicTopbarComponent ) );
        await configuration.RunSuccessfullyAsync();

        await using var runner = TestHelper.CreateTypeScriptRunner( targetProjectPath );
        await TestHelper.SuspendAsync( resume => resume );
        runner.Run();
    }
}
