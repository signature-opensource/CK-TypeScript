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

        [CKTypeDefiner]
        public interface ICommand : IPoco
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
        public interface ICommandThree : ICommand
        {
            int NumberThree { get; set; }
        }

        public interface ICommandFour : ICommand
        {
            int NumberFour { get; set; }
        }

        public class TypseScriptCommandSupportImpl : ITSCodeGenerator
        {
            public bool ConfigureTypeScriptAttribute( IActivityMonitor monitor,
                                                      TypeScriptGenerator generator,
                                                      Type type,
                                                      TypeScriptAttribute attr,
                                                      IList<ITSCodeGeneratorType> generatorTypes,
                                                      ref Func<IActivityMonitor, TSTypeFile, bool>? finalizer )
            {
                // All ICommand here (without specified TypeScript Folder) will be in Cris/Commands.
                // Their FileName will be without the "I" and prefixed by "CMD".
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
                    if( attr.FileName == null && attr.SameFileAs == null ) attr.FileName = "CMD" + type.Name.Substring( 1 ) + ".ts";
                }
                return true;
            }

            // The Cris command directory generator declares all the ICommand.
            public bool GenerateCode( IActivityMonitor monitor, TypeScriptGenerator g )
            {
                g.DeclareTSType( monitor, typeof( ICommandOne ), typeof( ICommandTwo ), typeof( ICommandThree ), typeof( ICommandFour ) );
                return true;
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
            var output = GenerateTSCode( "command_like_sample",
                                         typeof( ICommandOne ),
                                         typeof( ICommandTwo ),
                                         typeof( ICommandThree ),
                                         typeof( ICommandFour ),
                                         typeof( TypseScriptCommandSupport ) );

            var fPower = output.Combine( "CK/StObj/TypeScript/Tests/Power.ts" );
            var fOne = output.Combine( "TheFolder/CMDCommandOne.ts" );
            var fTwo = output.Combine( "Cris/Commands/TypeScript/Tests/CMDCommandTwo.ts" );

            File.ReadAllText( fPower ).Should().StartWith( "export enum Power" );
            var tOne = File.ReadAllText( fOne );
            tOne.Should().Contain( "import { Power } from '../CK/StObj/TypeScript/Tests/Power';" )
                     .And.Contain( "import { ICommandTwo } from '../Cris/Commands/TypeScript/Tests/CMDCommandTwo';" );

            tOne.Should().Contain( "export interface ICommandOne" )
                     .And.Contain( "friend: ICommandTwo;" );

            var tTwo = File.ReadAllText( fTwo );
            tTwo.Should().Contain( "import { ICommandOne, ICommandThree } from '../../../../TheFolder/CMDCommandOne';" );

        }









    }
}
