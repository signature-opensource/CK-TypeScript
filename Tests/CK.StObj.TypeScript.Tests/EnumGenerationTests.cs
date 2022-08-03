using CK.Core;
using CK.Setup;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using static CK.Testing.StObjEngineTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.StObj.TypeScript.Tests
{
    public class EnumGenerationTests
    {
        [TypeScript]
        public enum Simple
        {
            Zero,

            One,
        }

        static (NormalizedPath BinTSPath1, NormalizedPath BinTSPath2) GenerateTSCodeInB1AndB2Outputs( string testName, params Type[] types )
        {
            var output1 = TestHelper.CleanupFolder( LocalTestHelper.OutputFolder.AppendPart( testName ).AppendPart( "b1" ), false );
            var output2 = TestHelper.CleanupFolder( LocalTestHelper.OutputFolder.AppendPart( testName ).AppendPart( "b2" ), false );

            var config = new StObjEngineConfiguration() { ForceRun = true };
            config.Aspects.Add( new TypeScriptAspectConfiguration() );

            var b1 = new BinPathConfiguration();
            b1.AspectConfigurations.Add( new XElement( "TypeScript", new XAttribute( "OutputPath", output1 ) ) );
            var b2 = new BinPathConfiguration();
            b2.AspectConfigurations.Add( new XElement( "TypeScript", new XAttribute( "OutputPath", output2 ) ) );
            // b3 has no TypeScript aspect or no OutputPath or an empty OutputPath: nothing must be generated and this is just a warning.
            var b3 = new BinPathConfiguration();
            switch( Environment.TickCount % 3 )
            {
                case 0: b3.AspectConfigurations.Add( new XElement( "TypeScript", new XAttribute( "OutputPath", " " ) ) ); break;
                case 1: b3.AspectConfigurations.Add( new XElement( "TypeScript" ) ); break;
            }

            config.BinPaths.Add( b1 );
            config.BinPaths.Add( b2 );
            config.BinPaths.Add( b3 );

            var r = TestHelper.GetSuccessfulResult( TestHelper.CreateStObjCollector( types ) );
            StObjEngine.Run( TestHelper.Monitor, r, config ).Success.Should().BeTrue();

            Directory.Exists( output1 ).Should().BeTrue();
            Directory.Exists( output2 ).Should().BeTrue();

            return (output1, output2);
        }

        [Test]
        public void simple_enum_generation_in_multiple_BinPath()
        {
            var (output1, output2) = GenerateTSCodeInB1AndB2Outputs( "simple_enum_generation", typeof( Simple ) );

            var f1 = output1.Combine( "CK/StObj/TypeScript/Tests/Simple.ts" );
            var f2 = output2.Combine( "CK/StObj/TypeScript/Tests/Simple.ts" );
            File.Exists( f1 ).Should().BeTrue();
            File.Exists( f2 ).Should().BeTrue();

            var s = File.ReadAllText( f1 );
            s.Should().Contain( "export enum Simple" );
            s.Should().Be( File.ReadAllText( f2 ) );
        }

        /// <summary>
        /// Folder is explicitly "TheFolder".
        /// </summary>
        [TypeScript( Folder = "TheFolder" )]
        public enum InAnotherFolder : byte
        {
            /// <summary>
            /// Alpha.
            /// </summary>
            Alpha,

            /// <summary>
            /// Beta.
            /// </summary>
            Beta
        }

        [Test]
        public void explicit_Folder_configured()
        {
            var (output1, output2) = GenerateTSCodeInB1AndB2Outputs( "explicit_Folder_configured", typeof( InAnotherFolder ) );

            var f1 = output1.Combine( "TheFolder/InAnotherFolder.ts" );
            var f2 = output2.Combine( "TheFolder/InAnotherFolder.ts" );
            var s = File.ReadAllText( f1 );
            s.Should().Contain( "export enum InAnotherFolder" );
            s.Should().Be( File.ReadAllText( f2 ) );
        }

        /// <summary>
        /// Folder is explicitly set at the root (empty string).
        /// </summary>
        [TypeScript( Folder = "" )]
        public enum AtTheRootFolder : byte
        {
            /// <summary>
            /// Alpha.
            /// </summary>
            Alpha,

            /// <summary>
            /// Beta.
            /// </summary>
            Beta
        }

        [Test]
        public void empty_Folder_generates_code_at_the_Root()
        {
            var (output1, output2) = GenerateTSCodeInB1AndB2Outputs( "empty_Folder_generates_code_at_the_Root", typeof( AtTheRootFolder ) );

            var f1 = output1.Combine( "AtTheRootFolder.ts" );
            var f2 = output2.Combine( "AtTheRootFolder.ts" );
            var s = File.ReadAllText( f1 );
            s.Should().Contain( "export enum AtTheRootFolder" );
            s.Should().Be( File.ReadAllText( f2 ) );
        }

        /// <summary>
        /// This filename is explicitly "Folder/EnumFile.ts".
        /// </summary>
        [TypeScript( Folder = "Folder", FileName = "EnumFile.ts" )]
        public enum InASpecificFile : sbyte
        {
            /// <summary>
            /// Alpha.
            /// </summary>
            Alpha,

            /// <summary>
            /// Beta.
            /// </summary>
            Beta
        }

        [Test]
        public void explicit_FileName_configured()
        {
            var (output1, output2) = GenerateTSCodeInB1AndB2Outputs( "explicit_FileName_configured", typeof( InASpecificFile ) );

            var f1 = output1.Combine( "Folder/EnumFile.ts" );
            var f2 = output2.Combine( "Folder/EnumFile.ts" );
            var s = File.ReadAllText( f1 );
            s.Should().Contain( "export enum InASpecificFile" );
            s.Should().Be( File.ReadAllText( f2 ) );
        }

        /// <summary>
        /// The external name of this enumeration is "Toto" and its
        /// filename is explicitly "Folder/EnumFile.ts".
        /// </summary>
        [TypeScript( Folder = "Folder", FileName = "EnumFile.ts" )]
        [ExternalName("Toto")]
        public enum InASpecificFileWithAnExternalName : sbyte
        {
            /// <summary>
            /// Alpha.
            /// </summary>
            Alpha,

            /// <summary>
            /// Beta.
            /// </summary>
            Beta
        }

        [Test]
        public void ExternalName_attribute_overrides_the_Type_name()
        {
            var (output1, output2) = GenerateTSCodeInB1AndB2Outputs( "ExternalName_attribute_overrides_the_Type_name", typeof( InASpecificFileWithAnExternalName ) );

            var f1 = output1.Combine( "Folder/EnumFile.ts" );
            var f2 = output2.Combine( "Folder/EnumFile.ts" );
            var s = File.ReadAllText( f1 );
            s.Should().Contain( "export enum Toto" );
            s.Should().Be( File.ReadAllText( f2 ) );
        }

        /// <summary>
        /// The external name of this enumeration is "Toto".
        /// </summary>
        [TypeScript( Folder = "Folder" )]
        [ExternalName( "Toto" )]
        public enum WithAnExternalName : sbyte
        {
            /// <summary>
            /// The A is explicitly -2.
            /// </summary>
            A = -2,

            /// <summary>
            /// The C is not set: -1.
            /// </summary>
            C,

            /// <summary>
            /// The D is not set: 0.
            /// </summary>
            D,

            /// <summary>
            /// The E is explicitly 78.
            /// </summary>
            E = 78
        }

        [Test]
        public void ExternalName_attribute_overrides_the_Type_name_and_the_FileName()
        {
            var (output1, output2) = GenerateTSCodeInB1AndB2Outputs( "ExternalName_attribute_overrides_the_Type_name_and_the_FileName", typeof( WithAnExternalName ) );

            var f1 = output1.Combine( "Folder/Toto.ts" );
            var f2 = output2.Combine( "Folder/Toto.ts" );
            var s = File.ReadAllText( f1 );
            s.Should().Contain( "export enum Toto" );
            s.Should().Be( File.ReadAllText( f2 ) );
        }


        [TypeScript( Folder = "", FileName = "EnumFile.ts", TypeName = "EnumType" )]
        [ExternalName("ThisIsIgnoredSinceTypeNameIsDefined")]
        public enum AtTheRootAndWithAnotherExplicitTypeName : sbyte
        {
            A = -2,
            C,
            D,
            E = 78
        }

        [Test]
        public void explicit_TypeName_and_FileName_override_the_ExternalName()
        {
            var (output1, output2) = GenerateTSCodeInB1AndB2Outputs( "explicit_TypeName_and_FileName_override_the_ExternalName", typeof( AtTheRootAndWithAnotherExplicitTypeName ) );

            var f1 = output1.Combine( "EnumFile.ts" );
            var f2 = output2.Combine( "EnumFile.ts" );
            var s = File.ReadAllText( f1 );
            s.Should().Contain( "export enum EnumType" );
            s.Should().Be( File.ReadAllText( f2 ) );
        }

    }
}
