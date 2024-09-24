using CK.Setup;
using CK.Testing;
using NUnit.Framework;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.TS.JsonGraphSerializer.Tests;

[TestFixture]
public class TSTests
{
    [Test]
    public async Task CK_TS_JsonGraphSerializer_Async()
    {
        var targetProjectPath = TestHelper.GetTypeScriptInlineTargetProjectPath();

        // We don't need any C# backend here.
        var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
        engineConfig.FirstBinPath.Assemblies.Add( "CK.TS.JsonGraphSerializer" );
        var tsConfig = engineConfig.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath );
        tsConfig.GitIgnoreCKGenFolder = true;
        engineConfig.RunSuccessfully();
        
        // Runs the Jest tests.
        await using var runner = TestHelper.CreateTypeScriptRunner( targetProjectPath );
        runner.Run();
    }

}
