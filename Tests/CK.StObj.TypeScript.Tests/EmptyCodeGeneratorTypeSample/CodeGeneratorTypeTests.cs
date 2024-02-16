using CK.StObj.TypeScript.Tests.EmptyCodeGeneratorTypeSample;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.IO;
using static CK.Testing.StObjEngineTestHelper;

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
            var targetOutputPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();
            TestHelper.GenerateTypeScript( targetOutputPath, typeof( EnumThatWillBeEmpty ),
                                                             typeof( IWillBeEmpty ),
                                                             typeof( WillBeEmptyClass ),
                                                             typeof( WillBeEmptyStruct ) );

            var e = File.ReadAllText( targetOutputPath.Combine( "ck-gen/src/EnumThatWillBeEmpty.ts" ) );
            var i = File.ReadAllText( targetOutputPath.Combine( "ck-gen/src/IWillBeEmpty.ts" ) );
            var c = File.ReadAllText( targetOutputPath.Combine( "ck-gen/src/WillBeEmptyClass.ts" ) );
            var s = File.ReadAllText( targetOutputPath.Combine( "ck-gen/src/WillBeEmptyStruct.ts" ) );

            e.Should().Be( "export enum EnumThatWillBeEmpty {" + Environment.NewLine + "}" + Environment.NewLine );
            i.Should().Be( "export interface IWillBeEmpty {" + Environment.NewLine + "}" + Environment.NewLine );
            c.Should().Be( "export class WillBeEmptyClass {" + Environment.NewLine + "}" + Environment.NewLine );
            s.Should().Be( "export class WillBeEmptyStruct {" + Environment.NewLine + "}" + Environment.NewLine );

        }

    }
}
