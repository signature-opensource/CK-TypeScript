using CK.Core;
using CK.Setup;
using CK.StObj.TypeScript.Engine;
using CK.Testing;
using CK.Text;
using CK.TypeScript.CodeGen;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;
using static CK.Testing.StObjEngineTestHelper;

namespace CK.StObj.TypeScript.Tests
{
    [TestFixture]
    public class CommandLikeTests
    {
        static readonly NormalizedPath _outputFolder = TestHelper.TestProjectFolder.AppendPart( "TestOutput" );

        class MonoCollectorResolver : IStObjCollectorResultResolver
        {
            readonly Type[] _types;

            public MonoCollectorResolver( params Type[] types )
            {
                _types = types;
            }

            public StObjCollectorResult GetUnifiedResult( BinPathConfiguration unified )
            {
                return TestHelper.GetSuccessfulResult( TestHelper.CreateStObjCollector( _types ) );
            }

            public StObjCollectorResult GetSecondaryResult( BinPathConfiguration head, IEnumerable<BinPathConfiguration> all )
            {
                throw new NotImplementedException( "There is only one BinPath: only the unified one is required." );
            }

        }

        static NormalizedPath GenerateTSCode( string testName, params Type[] types )
        {
            var output = TestHelper.CleanupFolder( _outputFolder.AppendPart( testName ), false );
            var config = new StObjEngineConfiguration();
            config.Aspects.Add( new TypeScriptAspectConfiguration() );
            var b = new BinPathConfiguration();
            b.AspectConfigurations.Add( new XElement( "TypeScript", new XElement( "OutputPath", output ) ) );

            config.BinPaths.Add( b );

            var engine = new StObjEngine( TestHelper.Monitor, config );
            engine.Run( new MonoCollectorResolver( types ) ).Should().BeTrue( "StObjEngine.Run worked." );
            Directory.Exists( output ).Should().BeTrue();
            return output;
        }

        public interface ICommand
        {
            object CommandModel { get; }
        }

        [TypeScript( SameFolderAs = typeof(ICommandOne) )]
        public enum Power
        {
            None,
            Medium,
            Strong
        }

        [TypeScript( Folder = "TheFolder" )]
        public interface ICommandOne : ICommand
        {
            string Name { get; set; }

            Power Power { get; set; }

            ICommandTwo Friend { get; }
        }

        public interface ICommandTwo : ICommand
        {
            int Age { get; set; }

            Power AnotherPower { get; set; }

            ICommandOne AnotherFriend { get; set; }

            ICommandThree FriendThree { get; set; }
        }

        [TypeScript( SameFileAs = typeof( ICommandOne ) )]
        public interface ICommandThree : ICommandOne
        {
        }

        public class TypseScriptCommandSupportImpl : ITSCodeGenerator
        {
            public bool ConfigureTypeScriptAttribute( IActivityMonitor monitor, TypeScriptGenerator generator, Type type, TypeScriptAttribute attr, IReadOnlyList<ITSCodeGeneratorType> generatorTypes, ref ITSCodeGenerator currentHandler )
            {
                if( typeof(ICommand).IsAssignableFrom( type ) )
                {
                    if( attr.SameFolderAs == null )
                    {
                        attr.Folder ??= type.Namespace!.Replace( '.', '/' );

                        const string autoMapping = "CK/StObj/";
                        if( attr.Folder.StartsWith( autoMapping ) )
                        {
                            attr.Folder = "Cris/Commands/" + attr.Folder.Substring( autoMapping.Length );
                        }
                    }
                    if( attr.FileName == null && attr.SameFileAs == null ) attr.FileName = type.Name.Substring( 1 ) + ".ts";
                    // Takes control of all ICommand interfaces:
                    currentHandler = this;
                }
                return true;
            }

            public bool GenerateCode( IActivityMonitor monitor, TypeScriptGenerator g )
            {
                Generate( monitor, g, typeof( ICommandOne ) );
                Generate( monitor, g, typeof( ICommandTwo ) );
                Generate( monitor, g, typeof( ICommandThree ) );

                // Skip "object CommandModel" property for ICommand.
                var f = g.GetTSTypeFile( monitor, typeof( ICommand ) );
                f.EnsureFile().Body.Append( "export interface " ).Append( f.TypeName ).OpenBlock().CloseBlock();

                return true;
            }

            TSTypeFile Generate( IActivityMonitor monitor, TypeScriptGenerator g, Type i )
            {
                var f = g.GetTSTypeFile( monitor, i );
                var file = f.EnsureFile();
                file.Body.Append( "export interface " ).Append( f.TypeName );
                bool hasInterface = false;
                foreach( Type b in i.GetInterfaces() )
                {
                    if( !hasInterface )
                    {
                        file.Body.Append( " extends " );
                        hasInterface = true;
                    }
                    else file.Body.Append( ", " );
                    AppendTypeName( file.Body, monitor, g, b );
                }
                file.Body.OpenBlock();
                foreach( var p in i.GetProperties() )
                {
                    file.Body.Append( g.ToIdentifier( p.Name ) ).Append( ": " );
                    AppendTypeName( file.Body, monitor, g, p.PropertyType );
                    file.Body.Append( ";" ).NewLine();
                }
                file.Body.CloseBlock();
                return f;
            }

            void AppendTypeName( ITSFileBodySection body, IActivityMonitor monitor, TypeScriptGenerator g, Type t )
            {
                if( t == typeof( int ) || t == typeof( float ) || t == typeof( double ) ) body.Append( "number" );
                else if( t == typeof( bool ) ) body.Append( "boolean" );
                else if( t == typeof( string ) ) body.Append( "string" );
                else if( t == typeof( object ) ) body.Append( "unknown" );
                else
                {
                    var other = g.GetTSTypeFile( monitor, t );
                    body.File.Imports.EnsureImport( other.TypeName, other.EnsureFile() );
                    body.Append( other.TypeName );
                }
            }

        }

        // This static class is only here to trigger the global ITSCodeGenerator.
        [ContextBoundDelegation( "CK.StObj.TypeScript.Tests.CommandLikeTests+TypseScriptCommandSupportImpl, CK.StObj.TypeScript.Tests" )]
        public static class TypseScriptCommandSupport
        {
        }

        [Test]
        public void command_like_sample()
        {
            var output = GenerateTSCode( "command_like_sample", typeof( Power ), typeof( ICommandOne ), typeof( ICommandTwo ), typeof( ICommandThree ), typeof(TypseScriptCommandSupport) );

            var fPower = output.Combine( "TheFolder/Power.ts" );
            var fCommand = output.Combine( "Cris/Commands/TypeScript/Tests/CommandTwo.ts" );
            var fOne = output.Combine( "TheFolder/CommandOne.ts" );
            var fTwo = output.Combine( "Cris/Commands/TypeScript/Tests/CommandTwo.ts" );

            File.ReadAllText( fPower ).Should().StartWith( "export enum Power" );
            var tOne = File.ReadAllText( fOne );
            var tTwo = File.ReadAllText( fTwo );
        }









    }
}
