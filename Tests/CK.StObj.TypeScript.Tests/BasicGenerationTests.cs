using CK.Core;
using CK.Setup;
using CK.Testing;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using static CK.Testing.MonitorTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.StObj.TypeScript.Tests
{
    public class BasicGenerationTests
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
            var engineConfiguration = CreateConfigurationTSCodeInB1AndB2Outputs( targetProjectPath, typeof( Simple ) );
            engineConfiguration.RunSuccessfully();

            var f1 = targetProjectPath.Combine( "b1/ck-gen/src/CK/StObj/TypeScript/Tests/Simple.ts" );
            var f2 = targetProjectPath.Combine( "b2/ck-gen/src/CK/StObj/TypeScript/Tests/Simple.ts" );
            var f3 = targetProjectPath.Combine( "b3/ck-gen/src/CK/StObj/TypeScript/Tests/Simple.ts" );
            File.Exists( f1 ).Should().BeTrue();
            File.Exists( f2 ).Should().BeTrue();
            File.Exists( f3 ).Should().BeFalse();

            var s = File.ReadAllText( f1 );
            s.Should().Contain( "export enum Simple" );
            s.Should().Be( File.ReadAllText( f2 ) );

            static EngineConfiguration CreateConfigurationTSCodeInB1AndB2Outputs( NormalizedPath targetProjectPath, params Type[] types )
            {
                var output1 = TestHelper.CleanupFolder( targetProjectPath.AppendPart( "b1" ), false );
                var output2 = TestHelper.CleanupFolder( targetProjectPath.AppendPart( "b2" ), false );

                var config = new EngineConfiguration();
                config.AddAspect( new TypeScriptAspectConfiguration() );

                var tsB1 = new TypeScriptBinPathAspectConfiguration
                {
                    TargetProjectPath = output1,
                    SkipTypeScriptTooling = true
                };
                // The Simple enum has [TypeScript] attribute: it is useless to declare it as a
                // TSType.
                config.FirstBinPath.AddAspect( tsB1 );
                config.FirstBinPath.Types.Add( types );

                var b2 = new BinPathConfiguration();
                config.AddBinPath( b2 );
                var tsB2 = new TypeScriptBinPathAspectConfiguration
                {
                    TargetProjectPath = output2,
                    SkipTypeScriptTooling = true
                };
                tsB2.Types.AddRange( types.Select( t => new TypeScriptTypeConfiguration( t ) ) );
                b2.AddAspect( tsB2 );
                b2.Types.Add( types );

                // b3 has no TypeScript aspect or no TargetProjectPath or an empty TargetProjectPath:
                // nothing must be generated and this is just a warning.
                var b3 = new BinPathConfiguration();
                config.AddBinPath( b3 );
                switch( Environment.TickCount % 3 )
                {
                    case 0: b3.AddAspect( new TypeScriptBinPathAspectConfiguration() { TargetProjectPath = " " } ); break;
                    case 1: b3.AddAspect( new TypeScriptBinPathAspectConfiguration() ); break;
                }
                return config;
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

            // We don't need any C# backend here.
            var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
            engineConfig.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath, typeof( InAnotherFolder ) );
            engineConfig.FirstBinPath.Types.Add( typeof( InAnotherFolder ) );
            engineConfig.RunSuccessfully();

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
 
            // We don't need any C# backend here.
            var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
            engineConfig.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath, typeof( AtTheRootFolder ) );
            engineConfig.FirstBinPath.Types.Add( typeof( AtTheRootFolder ) );
            engineConfig.RunSuccessfully();

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

            // We don't need any C# backend here.
            var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
            engineConfig.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath, typeof( InASpecificFile ), typeof( AnotherInASpecificFile ) );
            engineConfig.FirstBinPath.Types.Add( typeof( InASpecificFile ), typeof( AnotherInASpecificFile ) );
            engineConfig.RunSuccessfully();

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
            // We don't need any C# backend here.
            var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
            engineConfig.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath, typeof( InASpecificFileWithAnExternalName ) );
            engineConfig.FirstBinPath.Types.Add( typeof( InASpecificFileWithAnExternalName ) );
            engineConfig.RunSuccessfully();

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
            // We don't need any C# backend here.
            var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
            engineConfig.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath, typeof( WithAnExternalName ) );
            engineConfig.FirstBinPath.Types.Add( typeof( WithAnExternalName ) );
            engineConfig.RunSuccessfully();

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

            // We don't need any C# backend here.
            var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
            engineConfig.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath, typeof( AtTheRootAndWithAnotherExplicitTypeName ) );
            engineConfig.FirstBinPath.Types.Add( typeof( AtTheRootAndWithAnotherExplicitTypeName ) );
            engineConfig.RunSuccessfully();

            var f = targetProjectPath.Combine( "ck-gen/src/EnumFile.ts" );
            var s = File.ReadAllText( f );
            s.Should().Contain( "export enum EnumType" );
        }

    }
}
