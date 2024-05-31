using CK.Core;
using CK.Setup;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using static CK.StObj.TypeScript.Tests.CommentTests;
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

        [Test]
        public void simple_enum_generation_in_multiple_BinPath()
        {
            var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();
            GenerateTSCodeInB1AndB2Outputs( targetProjectPath, typeof( Simple ) );

            var f1 = targetProjectPath.Combine( "b1/ck-gen/src/CK/StObj/TypeScript/Tests/Simple.ts" );
            var f2 = targetProjectPath.Combine( "b2/ck-gen/src/CK/StObj/TypeScript/Tests/Simple.ts" );
            var f3 = targetProjectPath.Combine( "b3/ck-gen/src/CK/StObj/TypeScript/Tests/Simple.ts" );
            File.Exists( f1 ).Should().BeTrue();
            File.Exists( f2 ).Should().BeTrue();
            File.Exists( f3 ).Should().BeFalse();

            var s = File.ReadAllText( f1 );
            s.Should().Contain( "export enum Simple" );
            s.Should().Be( File.ReadAllText( f2 ) );

            static void GenerateTSCodeInB1AndB2Outputs( NormalizedPath targetProjectPath, params Type[] types )
            {
                var output1 = TestHelper.CleanupFolder( targetProjectPath.AppendPart( "b1" ), false );
                var output2 = TestHelper.CleanupFolder( targetProjectPath.AppendPart( "b2" ), false );

                var config = new StObjEngineConfiguration();
                config.Aspects.Add( new TypeScriptAspectConfiguration() );

                var b1 = new BinPathConfiguration();
                var tsB1 = new TypeScriptBinPathAspectConfiguration
                {
                    TargetProjectPath = output1,
                    SkipTypeScriptTooling = true
                };
                tsB1.Types.AddRange( types.Select( t => new TypeScriptTypeConfiguration( t ) ) );
                b1.AspectConfigurations.Add( tsB1.ToXml() );

                var b2 = new BinPathConfiguration();
                var tsB2 = new TypeScriptBinPathAspectConfiguration
                {
                    TargetProjectPath = output2,
                    TypeFilterName = "TypeScript-B2Specific",
                    SkipTypeScriptTooling = true
                };
                tsB2.Types.AddRange( types.Select( t => new TypeScriptTypeConfiguration( t ) ) );
                b2.AspectConfigurations.Add( tsB2.ToXml() );

                // b3 has no TypeScript aspect or no TargetProjectPath or an empty TargetProjectPath:
                // nothing must be generated and this is just a warning.
                var b3 = new BinPathConfiguration();
                switch( Environment.TickCount % 3 )
                {
                    case 0: b3.AspectConfigurations.Add( new XElement( "TypeScript", new XAttribute( "TargetProjectPath", " " ) ) ); break;
                    case 1: b3.AspectConfigurations.Add( new XElement( "TypeScript" ) ); break;
                }

                config.BinPaths.Add( b1 );
                config.BinPaths.Add( b2 );
                config.BinPaths.Add( b3 );

                var r = TestHelper.GetSuccessfulResult( TestHelper.CreateStObjCollector( types ) );
                StObjEngine.Run( TestHelper.Monitor, r, config ).Success.Should().BeTrue();

                Directory.Exists( output1 ).Should().BeTrue();
                Directory.Exists( output2 ).Should().BeTrue();
            }

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
            var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();
            TestHelper.GenerateTypeScript( targetProjectPath, typeof( InAnotherFolder ) );

            var f = targetProjectPath.Combine( "ck-gen/src/TheFolder/InAnotherFolder.ts" );
            var s = File.ReadAllText( f );
            s.Should().Contain( "export enum InAnotherFolder" );
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
            var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();
            TestHelper.GenerateTypeScript( targetProjectPath, typeof( AtTheRootFolder ) );

            var f1 = targetProjectPath.Combine( "ck-gen/src/AtTheRootFolder.ts" );
            var s = File.ReadAllText( f1 );
            s.Should().Contain( "export enum AtTheRootFolder" );
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

        [TypeScript( Folder = "Folder", FileName = "EnumFile.ts", TypeName = "AInFile" )]
        public enum AnotherInASpecificFile : sbyte
        {
            Nop
        }

        [Test]
        public void explicit_FileName_configured()
        {
            var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();
            TestHelper.GenerateTypeScript( targetProjectPath, typeof( InASpecificFile ), typeof( AnotherInASpecificFile ) );

            var f1 = targetProjectPath.Combine( "ck-gen/src/Folder/EnumFile.ts" );
            var s = File.ReadAllText( f1 );
            s.Should().Contain( "export enum InASpecificFile" ).And.Contain( "export enum AInFile" );
        }

        /// <summary>
        /// The external name of this enumeration is "Toto" and its
        /// filename is explicitly "IAmHere/EnumFile.ts".
        /// </summary>
        [TypeScript( Folder = "IAmHere", FileName = "EnumFile.ts" )]
        [ExternalName( "Toto" )]
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
            var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();
            TestHelper.GenerateTypeScript( targetProjectPath, typeof( InASpecificFileWithAnExternalName ) );

            var f = targetProjectPath.Combine( "ck-gen/src/IAmHere/EnumFile.ts" );
            var s = File.ReadAllText( f );
            s.Should().Contain( "export enum Toto" );
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
        public void ExternalName_attribute_overrides_the_TypeName_and_the_FileName()
        {
            var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();
            TestHelper.GenerateTypeScript( targetProjectPath, typeof( WithAnExternalName ) );

            var f = targetProjectPath.Combine( "ck-gen/src/Folder/Toto.ts" );
            var s = File.ReadAllText( f );
            s.Should().Contain( "export enum Toto" );
        }

        [TypeScript( Folder = "", FileName = "EnumFile.ts", TypeName = "EnumType" )]
        [ExternalName( "ThisIsIgnoredSinceTypeNameIsDefined" )]
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
            var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();
            TestHelper.GenerateTypeScript( targetProjectPath, typeof( AtTheRootAndWithAnotherExplicitTypeName ) );

            var f = targetProjectPath.Combine( "ck-gen/src/EnumFile.ts" );
            var s = File.ReadAllText( f );
            s.Should().Contain( "export enum EnumType" );
        }

    }
}
