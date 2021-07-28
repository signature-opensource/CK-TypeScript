using CK.Text;
using CK.TypeScript.CodeGen;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static CK.Testing.StObjEngineTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.StObj.TypeScript.Tests.EmptyCodeGeneratorTypeSample
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
            var output = LocalTestHelper.GenerateTSCode( nameof( all_types_are_empty ),
                                                            typeof( EnumThatWillBeEmpty ),
                                                            typeof( IWillBeEmpty ),
                                                            typeof( WillBeEmptyClass ),
                                                            typeof( WillBeEmptyStruct ) );

            var e = File.ReadAllText( output.AppendPart( "EnumThatWillBeEmpty.ts" ) );
            var i = File.ReadAllText( output.AppendPart( "IWillBeEmpty.ts" ) );
            var c = File.ReadAllText( output.AppendPart( "WillBeEmptyClass.ts" ) );
            var s = File.ReadAllText( output.AppendPart( "WillBeEmptyStruct.ts" ) );

            e.Should().Be( "export enum EnumThatWillBeEmpty {" + Environment.NewLine + "}" + Environment.NewLine );
            i.Should().Be( "export interface IWillBeEmpty {" + Environment.NewLine + "}" + Environment.NewLine );
            c.Should().Be( "export class WillBeEmptyClass {" + Environment.NewLine + "}" + Environment.NewLine );
            s.Should().Be( "export class WillBeEmptyStruct {" + Environment.NewLine + "}" + Environment.NewLine );

        }

    }
}
