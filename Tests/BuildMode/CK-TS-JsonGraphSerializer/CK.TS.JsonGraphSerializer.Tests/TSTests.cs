using NUnit.Framework;
using System.Threading.Tasks;
using CK.Testing;
using static CK.Testing.StObjEngineTestHelper;
using CK.Setup;
using FluentAssertions;

namespace CK.TS.JsonGraphSerializer.Tests
{
    [TestFixture]
    public class TSTests
    {
        [Test]
        public async Task CK_TS_JsonGraphSerializer_Async()
        {
            // We don't need any C# backend here. Instead of this:
            //
            // var targetProjectPath = TestHelper.GetTypeScriptBuildModeTargetProjectPath();
            // TestHelper.RunSuccessfulEngineWithTypeScript( targetProjectPath, typeof( CK.JsonGraphSerializer.TSPackage ).Assembly );
            //
            // Skipping the Roslyn compilation saves 2 seconds.
            //
            var targetProjectPath = TestHelper.GetTypeScriptBuildModeTargetProjectPath();

            var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
            Testing.TypeScriptConfigurationExtensions.EnsureTypeScriptConfigurationAspect( TestHelper, engineConfig, targetProjectPath );
            var types = TestHelper.CreateTypeCollector( typeof( CK.JsonGraphSerializer.TSPackage ).Assembly );

            TestHelper.RunEngine( engineConfig, types ).Success.Should().BeTrue( "Engine.Run worked." );

            // Runs the Jest tests.
            await using var runner = TestHelper.CreateTypeScriptRunner( targetProjectPath );
            runner.Run();
        }

    }
}
