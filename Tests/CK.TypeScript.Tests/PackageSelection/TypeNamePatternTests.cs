using CK.Core;
using CK.Setup;
using CK.Testing;
using CK.TypeScript.Tests.PackageSelection;
using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.TypeScript.Tests.Globbing
{
    public interface IGlobbedPoco : IPoco
    {
        int Power { get; set; }
    }

    /// <summary>
    /// A regular class: a pattern ignores it (an explicit type would require a TypeScript code generator).
    /// </summary>
    public sealed class GlobbedHelper { }

    public enum EnumG { G }

    [TypeScriptPackage]
    [RegisterTypeScriptType( typeof( EnumG ) )]
    public sealed class GlobbedPackage : TypeScriptPackage { }
}

namespace CK.TypeScript.Tests.PackageSelection
{
    using Globbing;

    [TestFixture]
    public class TypeNamePatternTests
    {
        static EngineConfiguration CreateConfiguration( out TypeScriptBinPathAspectConfiguration ts, NormalizedPath targetProjectPath )
        {
            var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
            engineConfig.FirstBinPath.Types.Add( typeof( PackageA ), typeof( PackageB ), typeof( PackageC ),
                                                 typeof( IGlobbedPoco ), typeof( GlobbedHelper ), typeof( GlobbedPackage ) );
            engineConfig.EnsureAspect<TypeScriptAspectConfiguration>();
            ts = engineConfig.FirstBinPath.EnsureAspect<TypeScriptBinPathAspectConfiguration>();
            ts.TargetProjectPath = targetProjectPath;
            ts.IntegrationMode = CKGenIntegrationMode.None;
            return engineConfig;
        }

        static string[] GeneratedFiles( NormalizedPath targetProjectPath, string folder )
        {
            var f = targetProjectPath.Combine( "ck-gen/CK/TypeScript/Tests/" + folder );
            return Directory.Exists( f )
                    ? Directory.GetFiles( f, "*.ts" ).Select( p => Path.GetFileNameWithoutExtension( p ) ).Order().ToArray()
                    : [];
        }

        [Test]
        public async Task a_pattern_registers_the_Poco_types_and_lists_the_packages_Async()
        {
            var targetProjectPath = TestHelper.CleanupFolder( TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath(), false );
            var engineConfig = CreateConfiguration( out var ts, targetProjectPath );
            ts.GlobTypes.Add( new TypeScriptTypeGlobConfiguration( TypeNamePattern.Parse( "CK.TypeScript.Tests.Globbing.*, CK.TypeScript.Tests" ) ) );
            await engineConfig.RunSuccessfullyAsync();

            // The IGlobbedPoco is registered, the GlobbedPackage is listed (its EnumG is registered), the GlobbedHelper is ignored.
            var globbed = GeneratedFiles( targetProjectPath, "Globbing" );
            globbed.ShouldContain( "EnumG" );
            globbed.ShouldContain( n => n.Contains( "GlobbedPoco" ) );
            // A package is listed: the other packages are not selected.
            GeneratedFiles( targetProjectPath, "PackageSelection" ).ShouldBeEmpty();
        }

        [Test]
        public async Task an_excluded_type_pattern_excludes_the_packages_Async()
        {
            var targetProjectPath = TestHelper.CleanupFolder( TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath(), false );
            var engineConfig = CreateConfiguration( out var ts, targetProjectPath );
            ts.ExcludedTypePatterns.Add( TypeNamePattern.Parse( "CK.TypeScript.Tests.PackageSelection.PackageC*, CK.TypeScript.Tests" ) );
            await engineConfig.RunSuccessfullyAsync();

            GeneratedFiles( targetProjectPath, "PackageSelection" ).ShouldBe( ["EnumA", "EnumB"] );
            // Without a pattern, the IGlobbedPoco is not registered.
            GeneratedFiles( targetProjectPath, "Globbing" ).ShouldBe( ["EnumG"] );
        }

        [Test]
        public void a_pattern_can_only_configure_the_Folder()
        {
            var pattern = TypeNamePattern.Parse( "CK.TypeScript.Tests.Globbing.*, CK.TypeScript.Tests" );
            new TypeScriptTypeGlobConfiguration( pattern, new TypeScriptTypeAttribute { Folder = "Globbed" } ).Configuration!.Folder.ShouldBe( "Globbed" );
            Util.Invokable( () => new TypeScriptTypeGlobConfiguration( pattern, new TypeScriptTypeAttribute { TypeName = "X" } ) )
                .ShouldThrow<ArgumentException>()
                .Message.ShouldContain( "Only the Folder can be configured for the type name pattern 'CK.TypeScript.Tests.Globbing.*, CK.TypeScript.Tests'." );
        }
    }
}
