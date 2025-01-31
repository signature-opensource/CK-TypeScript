using CK.Setup;
using CK.Testing;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CK.Setup.EngineResult;
using static CK.Testing.MonitorTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.TypeScript.Tests;


[TestFixture]
public class TypeScriptPackageTests
{
    [TypeScriptPackage]
    [SpecializedPackage]
    public class SomePackage : TypeScriptPackage
    {
    }

    [Test]
    public async Task single_TypeScriptPackage_attribute_is_allowed_Async()
    {
        var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
        engineConfig.FirstBinPath.Types.Add( typeof( SomePackage ) );
        engineConfig.EnsureAspect<TypeScriptAspectConfiguration>();
        engineConfig.FirstBinPath.EnsureAspect<TypeScriptBinPathAspectConfiguration>();

        await engineConfig.GetFailedAutomaticServicesAsync( """
                    TypeScript package 'CK.TypeScript.Tests.TypeScriptPackageTests.SomePackage' is decorated with more than one [TypeScriptPackage] or specialized attribute:
                    [TypeScriptPackageAttribute], [SpecializedPackageAttribute]
                    """ );
    }

    [TypeScriptPackage]
    public class PackageA : TypeScriptPackage
    {
    }

    [TypeScriptPackage]
    public class PackageB : PackageA
    {
    }


    [Test]
    public async Task TypeScriptPackage_cannot_be_specialized_Async()
    {
        var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
        engineConfig.FirstBinPath.Types.Add( typeof( PackageA ), typeof( PackageB ) );
        engineConfig.EnsureAspect<TypeScriptAspectConfiguration>();
        engineConfig.FirstBinPath.EnsureAspect<TypeScriptBinPathAspectConfiguration>();

        await engineConfig.GetFailedAutomaticServicesAsync( """
                    TypeScript package 'CK.TypeScript.Tests.TypeScriptPackageTests.SomePackage' is decorated with more than one [TypeScriptPackage] or specialized attribute:
                    [TypeScriptPackageAttribute], [SpecializedPackageAttribute]
                    """ );
    }
}
