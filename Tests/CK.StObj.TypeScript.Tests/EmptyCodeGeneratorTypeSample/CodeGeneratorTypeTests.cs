using CK.Setup;
using CK.StObj.TypeScript.Tests.EmptyCodeGeneratorTypeSample;
using CK.Testing;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.IO;
using static CK.Testing.MonitorTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.StObj.TypeScript.Tests
{

    [TestFixture]
    public class CodeGeneratorTypeTests
    {
        [EmptyTypeScript( Folder = "" )]
        public enum EnumThatWillBeEmpty
        {
            Zero,
            One,
            Two
        }

        [EmptyTypeScript( Folder = "" )]
        public interface IWillBeEmpty
        {
            string DontCare { get; }
        }

        [EmptyTypeScript( Folder = "" )]
        public class WillBeEmptyClass
        {
            public string? DontCare { get; set; }
        }

        [EmptyTypeScript( Folder = "" )]
        public struct WillBeEmptyStruct
        {
            public string? DontCare { get; set; }
        }

        [Test]
        public void all_types_are_empty()
        {
            var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();

            var types = new[]
{
                typeof( EnumThatWillBeEmpty ),
                typeof( IWillBeEmpty ),
                typeof( WillBeEmptyClass ),
                typeof( WillBeEmptyStruct )
            };

            // We don't need any C# backend here.
            var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
            engineConfig.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath, types );
            engineConfig.FirstBinPath.Types.Add( types );
            engineConfig.RunSuccessfully();


            var e = File.ReadAllText( targetProjectPath.Combine( "ck-gen/EnumThatWillBeEmpty.ts" ) );
            var i = File.ReadAllText( targetProjectPath.Combine( "ck-gen/IWillBeEmpty.ts" ) );
            var c = File.ReadAllText( targetProjectPath.Combine( "ck-gen/WillBeEmptyClass.ts" ) );
            var s = File.ReadAllText( targetProjectPath.Combine( "ck-gen/WillBeEmptyStruct.ts" ) );

            e.Should().Be( "export enum EnumThatWillBeEmpty {" + Environment.NewLine + "}" + Environment.NewLine );
            i.Should().Be( "export interface IWillBeEmpty {" + Environment.NewLine + "}" + Environment.NewLine );
            c.Should().Be( "export class WillBeEmptyClass {" + Environment.NewLine + "}" + Environment.NewLine );
            s.Should().Be( "export class WillBeEmptyStruct {" + Environment.NewLine + "}" + Environment.NewLine );

        }

    }
}
