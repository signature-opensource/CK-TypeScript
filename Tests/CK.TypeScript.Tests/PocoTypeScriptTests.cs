using CK.Core;
using CK.Setup;
using CK.Testing;
using FluentAssertions;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.TypeScript.Tests;


[TestFixture]
public class PocoTypeScriptTests
{
    [ExternalName( "NotGeneratedByDefault" )]
    public interface INotGeneratedByDefault : IPoco
    {
        int Power { get; set; }
    }

    /// <summary>
    /// IPoco are not automatically exported.
    /// Using the [TypeScript] attribute declares the type.
    /// </summary>
    [TypeScript]
    public interface IGeneratedByDefault : IPoco
    {
        INotGeneratedByDefault Some { get; set; }
    }

    [Test]
    public async Task no_TypeScript_attribute_provide_no_generation_Async()
    {
        // NotGeneratedByDefault is not generated.
        var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();

        // We don't need any C# backend here.
        var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
        engineConfig.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath );
        engineConfig.FirstBinPath.Types.Add( typeof( INotGeneratedByDefault ) );
        await engineConfig.RunSuccessfullyAsync();

        File.Exists( targetProjectPath.Combine( "ck-gen/src/CK.TypeScript/Tests/NotGeneratedByDefault.ts" ) ).Should().BeFalse();
    }

    [Test]
    public async Task no_TypeScript_attribute_is_generated_when_referenced_Async()
    {
        // NotGeneratedByDefault is generated because it is referenced by IGeneratedByDefault.
        var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();

        // We don't need any C# backend here.
        var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
        engineConfig.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath );
        engineConfig.FirstBinPath.Types.Add( typeof( IGeneratedByDefault ), typeof( INotGeneratedByDefault ) );
        await engineConfig.RunSuccessfullyAsync();

        File.Exists( targetProjectPath.Combine( "ck-gen/CK/TypeScript/Tests/NotGeneratedByDefault.ts" ) ).Should().BeTrue();
    }

    [Test]
    public async Task no_TypeScript_attribute_is_generated_when_Type_appears_in_Aspect_Async()
    {
        // NotGeneratedByDefault is generated because it is configured.
        var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();
        // We don't need any C# backend here.
        var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
        engineConfig.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath, typeof( INotGeneratedByDefault ) );
        engineConfig.FirstBinPath.Types.Add( typeof( INotGeneratedByDefault ) );
        await engineConfig.RunSuccessfullyAsync();

        File.ReadAllText( targetProjectPath.Combine( "ck-gen/CK/TypeScript/Tests/NotGeneratedByDefault.ts" ) )
            .Should().Contain( "export class NotGeneratedByDefault" );
    }

    public interface IRecursive : IPoco
    {
        IList<IRecursive> Children { get; }
    }

    [Test]
    public async Task recursive_type_dont_import_themselves_Async()
    {
        var targetProjectPath = TestHelper.GetTypeScriptNpmPackageTargetProjectPath();
        var engineConfig = TestHelper.CreateDefaultEngineConfiguration();
        engineConfig.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath, typeof( IRecursive ) );
        engineConfig.FirstBinPath.Types.Add( typeof( IRecursive ) );
        await engineConfig.RunSuccessfullyAsync();
    }
}
