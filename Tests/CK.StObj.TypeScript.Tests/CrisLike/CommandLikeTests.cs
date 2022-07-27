using CK.Core;
using CK.Setup;
using CK.StObj.TypeScript.Engine;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.StObj.TypeScript.Tests.CrisLike
{
    [TestFixture]
    public class CommandLikeTests
    {
        [TypeScript( SameFolderAs = typeof(ICommandOne) )]
        public enum Power
        {
            /// <summary>
            /// No Power.
            /// </summary>
            None,

            /// <summary>
            /// Intermediate power.
            /// </summary>
            Medium,

            /// <summary>
            /// Full power.
            /// </summary>
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

            DateTime StartDate { get; set; }
        }

        public interface ICommandFour : ICommand
        {
            int NumberFour { get; set; }

            Guid? UniqueId { get; set; }
        }

        // Hard coded Cris-like CommandDirectoryImpl.
        // This one changes the folders.
        public class FakeCommandDirectoryImplWithFolders : ITSCodeGenerator
        {
            public bool ConfigureTypeScriptAttribute( IActivityMonitor monitor,
                                                      ITSTypeFileBuilder builder,
                                                      TypeScriptAttribute attr )
            {
                // All ICommand here (without specified TypeScript Folder) will be in Cris/Commands.
                // Their FileName will be without the "I" and prefixed by "CMD".
                // The real CommandDirectoryImpl does nothing here: ICommand are IPoco and
                // their folder/file organization is fine.

                if( typeof(ICommand).IsAssignableFrom( builder.Type ) )
                {
                    if( attr.SameFolderAs == null )
                    {
                        attr.Folder ??= builder.Type.Namespace!.Replace( '.', '/' );

                        const string autoMapping = "CK/StObj/";
                        if( attr.Folder.StartsWith( autoMapping ) )
                        {
                            attr.Folder = "Cris/Commands/" + attr.Folder.Substring( autoMapping.Length );
                        }
                    }
                    if( attr.FileName == null && attr.SameFileAs == null ) attr.FileName = "CMD" + builder.Type.Name.Substring( 1 ) + ".ts";
                }
                return true;
            }

            // The Cris command directory generator declares all the ICommand (by filtering the declared poco). This one
            // declares them explicitly.
            // ICommandResult and any types that are exposed from the ICommand are exported by the IPoco TS engine.
            public bool GenerateCode( IActivityMonitor monitor, TypeScriptContext g )
            {
                g.DeclareTSType( monitor, typeof( ICommandResult ) );
                g.DeclareTSType( monitor, typeof( ICommandOne ), typeof( ICommandTwo ), typeof( ICommandThree ), typeof( ICommandFour ) );
                return true;
            }
        }

        // This static class is only here to trigger the global TypeScriptCommandSupportImpl ITSCodeGenerator.
        [ContextBoundDelegation( "CK.StObj.TypeScript.Tests.CrisLike.CommandLikeTests+FakeCommandDirectoryImplWithFolders, CK.StObj.TypeScript.Tests" )]
        public static class FakeCommandDirectoryWithFolders
        {
        }

        [Test]
        public void command_like_sample_with_interfaces()
        {
            var output = LocalTestHelper.GenerateTSCode( nameof( command_like_sample_with_interfaces ),
                                                         new TypeScriptAspectConfiguration() { GeneratePocoInterfaces = true },
                                                         typeof( ICommandOne ),
                                                         typeof( ICommandTwo ),
                                                         typeof( ICommandThree ),
                                                         typeof( ICommandFour ),
                                                         typeof( ICommandResult ),
                                                         typeof( FakeCommandDirectoryWithFolders ) );

            var fPower = output.Combine( "TheFolder/Power.ts" );
            File.ReadAllText( fPower ).Should().StartWith( "export enum Power" );

            var fOne = output.Combine( "TheFolder/CommandOne.ts" );
            var tOne = File.ReadAllText( fOne );
            tOne.Should().Contain( "import { Power } from './Power';" )
                     .And.Contain( "import { CommandTwo } from '../Cris/Commands/TypeScript/Tests/CrisLike/CommandTwo';" );

            tOne.Should().Contain( "export interface ICommandOne" )
                     .And.Contain( "friend: CommandTwo;" );


            var fTwo = output.Combine( "Cris/Commands/TypeScript/Tests/CrisLike/CommandTwo.ts" );
            var tTwo = File.ReadAllText( fTwo );
            tTwo.Should().Contain( "import { CommandOne, CommandThree } from '../../../../../TheFolder/CommandOne';" );
        }

        public class FakeCommandDirectoryImpl : ITSCodeGenerator
        {
            public bool ConfigureTypeScriptAttribute( IActivityMonitor monitor,
                                                      ITSTypeFileBuilder builder,
                                                      TypeScriptAttribute attr )
            {
                if( attr.SameFolderAs == null && attr.Folder == null )
                {
                    if( typeof( ICommand ).IsAssignableFrom( builder.Type ) )
                    {
                        attr.Folder = "Cris/Commands";
                    }
                    else
                    {
                        attr.Folder = "Other";
                    }
                }
                return true;
            }

            // The Cris command directory generator declares all the ICommand.
            public bool GenerateCode( IActivityMonitor monitor, TypeScriptContext g )
            {
                var poco = g.CodeContext.CurrentRun.ServiceContainer.GetService<IPocoSupportResult>( true );
                if( poco.OtherInterfaces.TryGetValue( typeof(ICommand), out var roots ) )
                {
                    foreach( var rootInfo in roots )
                    {
                        g.DeclareTSType( monitor, rootInfo.Interfaces.Select( itf => itf.PocoInterface ) );
                        var resultTypes = rootInfo.OtherInterfaces.Select( i => ExtractTResult( i ) )
                                                                  .Where( r => r != null )
                                                                  .Select( t => t! )
                                                                  .ToList();
                        g.DeclareTSType( monitor, resultTypes );
                    }

                    static Type? ExtractTResult( Type i )
                    {
                        if( !i.IsGenericType ) return null;
                        Type tG = i.GetGenericTypeDefinition();
                        if( tG != typeof( ICommand<> ) ) return null;
                        return i.GetGenericArguments()[0];
                    }

                }
                return true;
            }
        }

        /// <summary>
        /// Triggers the <see cref="FakeCommandDirectoryImpl"/>.
        /// </summary>
        [ContextBoundDelegation( "CK.StObj.TypeScript.Tests.CrisLike.CommandLikeTests+FakeCommandDirectoryImpl, CK.StObj.TypeScript.Tests" )]
        public static class FakeCommandDirectory
        {
        }

        public interface IValueTupleCommand : ICommandAuthUnsafe, ICommand<int>
        {
            (int, string, string?, object, List<string?>, object?) Power { get; set; }
        }

        [Test]
        public void command_with_ValueTuple()
        {
            var output = LocalTestHelper.GenerateTSCode( nameof( command_with_ValueTuple ),
                                                         typeof( IValueTupleCommand ),
                                                         typeof( FakeCommandDirectory ) );
        }

        /// <summary>
        /// This command requires authentication and is device dependent.
        /// It returns an optional object as its result.
        /// </summary>
        public interface IWithObjectCommand : ICommandAuthDeviceId, ICommand<object?>
        {
            /// <summary>
            /// Gets the power of this command.
            /// </summary>
            int? Power { get; set; }
        }

        /// <summary>
        /// This command extends <see cref="IWithObjectCommand"/> with the power of the string.
        /// </summary>
        public interface IWithObjectSpecializedAsStringCommand : IWithObjectCommand, ICommand<string>
        {
            /// <summary>
            /// Gets the power of the string.
            /// <para>
            /// The string has a great power!
            /// </para>
            /// </summary>
            int PowerString { get; set; }
        }

        [Test]
        public void command_with_simple_results_specialized()
        {
            var output = LocalTestHelper.GenerateTSCode( nameof( command_with_simple_results_specialized ),
                                                         typeof( IValueTupleCommand ),
                                                         typeof( IWithObjectCommand ),
                                                         typeof( IWithObjectSpecializedAsStringCommand ),
                                                         typeof( FakeCommandDirectory ) );

        }

        public interface IResult : IPoco
        {
            int Result { get; set; }
        }

        public interface ISuperResult : IResult
        {
            string SuperResult { get; set; }
        }

        public interface IWithObjectSpecializedAsPocoCommand : IWithObjectCommand, ICommandAuthDeviceId, ICommand<IResult>
        {
            int PowerPoco { get; set; }
        }

        public interface IWithObjectSpecializedAsSuperPocoCommand : IWithObjectCommand, ICommand<ISuperResult>
        {
        }

        [Test]
        public void command_with_poco_results_specialized_and_parts()
        {
            var output = LocalTestHelper.GenerateTSCode( nameof( command_with_poco_results_specialized_and_parts ),
                                         typeof( IWithObjectCommand ),
                                         typeof( IWithObjectSpecializedAsPocoCommand ),
                                         typeof( IResult ),
                                         typeof( IWithObjectSpecializedAsSuperPocoCommand ),
                                         typeof( ISuperResult ),
                                         typeof( FakeCommandDirectory ) );

        }

    }
}
