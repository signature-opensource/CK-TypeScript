using CK.Setup;
using CK.Testing;
using NUnit.Framework;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.TypeScript.Tests;


[TestFixture]
public class TypeScriptPackageTests
{
    [TypeScriptPackage]
    [SpecializedPackage]
    public class SomeBuggyPackage : TypeScriptPackage
    {
    }

    [Test]
    public async Task single_TypeScriptPackage_attribute_is_allowed_Async()
    {
        var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
        engineConfig.FirstBinPath.Types.Add( typeof( SomeBuggyPackage ) );
        engineConfig.EnsureAspect<TypeScriptAspectConfiguration>();
        engineConfig.FirstBinPath.EnsureAspect<TypeScriptBinPathAspectConfiguration>();

        await engineConfig.GetFailedAutomaticServicesAsync( """
                    TypeScript package 'CK.TypeScript.Tests.TypeScriptPackageTests.SomeBuggyPackage' is decorated with more than one [TypeScriptPackage] or specialized attribute:
                    [TypeScriptPackageAttribute], [SpecializedPackageAttribute]
                    """ );
    }

}
