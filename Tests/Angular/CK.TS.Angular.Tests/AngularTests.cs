using CK.Core;
using CK.MiscDemo;
using CK.Ng.PublicSection;
using CK.Ng.AspNet.Auth;
using CK.Testing;
using NUnit.Framework;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;
using CK.Ng.PublicPage;

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

        configuration.FirstBinPath.Assemblies.Add( "CK.TS.Angular" );
        configuration.FirstBinPath.Types.Add( typeof( DemoNgModule ),
                                              typeof( AppRoutedComponent ),
                                              // CK.Ng.PublicPage.
                                              typeof( PublicPageComponent ),
                                              // CK.Ng.AspNet.Auth folder.
                                              typeof( LoginComponent ),
                                              typeof( PasswordLostComponent ),
                                              typeof( LogoutConfirmComponent ),
                                              typeof( LogoutResultComponent ),
                                              typeof( SomeAuthPackage ),
                                              // CK.Ng.PublicSection
                                              typeof( PublicSectionPackage ),
                                              typeof( PublicFooterComponent ),
                                              typeof( PublicTopbarComponent ),
                                              // CK.Ng.Zorro
                                              typeof( CK.Ng.Zorro.TSPackage )
                                            );
        await configuration.RunSuccessfullyAsync();

        await using var runner = TestHelper.CreateTypeScriptRunner( targetProjectPath );
        await TestHelper.SuspendAsync( resume => resume );
        runner.Run();
    }
}
