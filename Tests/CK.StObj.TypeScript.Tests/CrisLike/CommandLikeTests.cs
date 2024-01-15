using CK.Core;
using CK.CrisLike;
using CK.Setup;
using CK.TypeScript.CodeGen;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using static CK.StObj.TypeScript.Tests.CrisLike.CommandLikeTests;
using static CK.Testing.StObjEngineTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.StObj.TypeScript.Tests.CrisLike
{
    [TestFixture]
    public class CommandLikeTests
    {
        /// <summary>
        /// Power level.
        /// </summary>
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

        /// <summary>
        /// The command n°1 has a <see cref="Friend"/> command.
        /// </summary>
        [TypeScript( Folder = "TheFolder" )]
        public interface ICommandOne : ICommand
        {
            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            string Name { get; set; }

            /// <summary>
            /// Gets or sets the power.
            /// </summary>
            Power Power { get; set; }

            /// <summary>
            /// Gets the friend.
            /// </summary>
            ICommandTwo Friend { get; }
        }

        public interface ICommandTwo : ICommand
        {
            int Age { get; set; }

            Power AnotherPower { get; set; }

            ICommandOne? AnotherFriend { get; set; }

            ICommandThree FriendThree { get; set; }
        }

        [TypeScript( SameFileAs = typeof( ICommandOne ) )]
        public interface ICommandThree : ICommand
        {
            int NumberThree { get; set; }

            DateTime StartDate { get; set; }
        }

        /// <summary>
        /// Simple record data.
        /// </summary>
        /// <param name="Index">The data index.</param>
        /// <param name="SuperName">A great name for the data.</param>
        public record struct RecordData( int Index, string SuperName );

        /// <summary>
        /// This one tests the record comments.
        /// </summary>
        public interface ICommandFour : ICommand
        {
            /// <summary>
            /// Gets or sets a unique identifier.
            /// </summary>
            Guid? UniqueId { get; set; }

            /// <summary>
            /// The record data.
            /// </summary>
            ref RecordData Data { get; }
        }

        public record MinimalCommandModel( IPocoType? ResultType );

        // Hard coded fake CommandDirectoryImpl: resolves the hopefully unique TResult of ICommand<>
        // but doesn't generate the C# code of the FakeCommandDirectoryImpl that is
        // a static class here.
        public class FakeCommandDirectoryImpl : ICSCodeGenerator
        {
            public static readonly Dictionary<IPrimaryPocoType, MinimalCommandModel> _models = new();
            public CSCodeGenerationResult Implement( IActivityMonitor monitor, ICSCodeGenerationContext codeGenContext )
            {
                _models.Clear();
                bool success = true;
                var pocoTypeSystem = codeGenContext.Assembly.GetPocoTypeSystem();
                var allCommands = pocoTypeSystem.FindByType<IAbstractPocoType>( typeof( IAbstractCommand ) )?.PrimaryPocoTypes;
                if( allCommands == null || !allCommands.Any() )
                {
                    monitor.Warn( $"No ICommand found." );
                }
                else
                {
                    foreach( var c in allCommands )
                    {
                        if( c.IsExchangeable )
                        {
                            var resultTypes = c.MinimalAbstractTypes.Where( a => a.GenericTypeDefinition?.Type == typeof( ICommand<> ) )
                                                                    .Select( a => a.GenericArguments[0].Type )
                                                                    .ToList();
                            IPocoType? resultType = null;
                            if( resultTypes.Count > 0 )
                            {
                                if( resultTypes.Count > 1 )
                                {
                                    var conflicts = resultTypes.Select( r => r.ToString() ).Concatenate();
                                    monitor.Error( $"Command Result Type conflict for '{c}': {conflicts}" );
                                    success = false;
                                }
                                resultType = resultTypes[0];
                            }
                            if( success )
                            {
                                _models.Add( c, new MinimalCommandModel( resultType ) );
                            }
                        }
                    }
                }
                return success ? CSCodeGenerationResult.Success : CSCodeGenerationResult.Failed;
            }
        }

        /// <summary>
        /// Triggers the <see cref="FakeCommandDirectoryImpl"/>.
        /// In actual code this is a ISingletonAutoService that exposes all the ICommandModel.
        /// </summary>
        [ContextBoundDelegation( "CK.StObj.TypeScript.Tests.CrisLike.CommandLikeTests+FakeCommandDirectoryImpl, CK.StObj.TypeScript.Tests" )]
        public static class FakeCommandDirectory
        {
            // This is pure fake of course...
            public static IDictionary<IPrimaryPocoType, MinimalCommandModel> Commands => FakeCommandDirectoryImpl._models;
        }



        // Hard coded Cris-like TypeScriptCrisCommandGeneratorImpl.
        public class FakeTypeScriptCrisCommandGeneratorImpl : ITSCodeGenerator
        {
            TSType? _command;

            public virtual bool Initialize( IActivityMonitor monitor, TypeScriptContext context )
            {
                // This can be called IF multiple contexts must be generated:
                // we reset the cached instance here.
                _command = null;
                return true;
            }

            public virtual bool OnResolveObjectKey( IActivityMonitor monitor, TypeScriptContext context, TSTypeRequiredEventArgs e )
            {
                // We don't add anything to the default IPocoType handling.
                return true;
            }

            public virtual bool GenerateCode( IActivityMonitor monitor, TypeScriptContext g )
            {
                // We don't generate global code.
                return true;
            }

            public virtual bool OnResolveType( IActivityMonitor monitor,
                                               TypeScriptContext context,
                                               TypeBuilderRequiredEventArgs builder )
            {
                var t = builder.Type;
                if( t.Namespace == "CK.CrisLike" && (t.Name == "ICommand" || (t.IsGenericTypeDefinition && t.Name == "ICommand`1")) )
                {
                    builder.ResolvedType = EnsureCrisCommandModel( monitor, context.Root );
                }
                return true;
            }

            TSType EnsureCrisCommandModel( IActivityMonitor monitor, TypeScriptRoot root )
            {
                if( _command != null ) return _command;

                var modelFile = root.Root.FindOrCreateFile( "CK/Cris/Model.ts" );
                GenerateCrisModelFile( monitor, modelFile );
                //GenerateCrisEndpoint( monitor, modelFile.Folder.FindOrCreateFile( "CrisEndpoint.ts" ) );
                //GenerateCrisHttpEndpoint( monitor, modelFile.Folder.FindOrCreateFile( "HttpCrisEndpoint.ts" ) );
                return _command = new TSType( "ICommand", imports => imports.EnsureImport( modelFile, "ICommand" ), null );

                static void GenerateCrisModelFile( IActivityMonitor monitor, TypeScriptFile fModel )
                {
                    fModel.Imports.EnsureImport( monitor, typeof( SimpleUserMessage ) );
                    fModel.Imports.EnsureImport( monitor, typeof( UserMessageLevel ) );

                    fModel.Body.Append( """
                                /**
                                 * Describes a command. 
                                 **/
                                export type CommandModel<TResult> = {
                                    /**
                                     * Gets the name of the command. 
                                     **/
                                    readonly commandName: string;
                                    /**
                                     * This supports the CrisEdpoint implementation. This is not to be used directly.
                                     **/
                                    readonly applyAmbientValues: (command: any, a: any, o: any ) => void;
                                }

                                /** 
                                 * Command abstraction: command with or without a result. 
                                 * **/
                                export interface ICommand<TResult = void> { 
                                    /**
                                     * Gets the command description. 
                                     **/
                                    readonly commandModel: CommandModel<TResult>;
                                }

                                /** 
                                 * Captures the result of a command execution.
                                 **/
                                export type ExecutedCommand<T> = {
                                    /** The executed command. **/
                                    readonly command: ICommand<T>,
                                    /** The execution result. **/
                                    readonly result: CrisError | T,
                                    /** Optional correlation identifier. **/
                                    readonly correlationId?: string
                                };

                                /**
                                 * Captures communication, validation or execution error.
                                 **/
                                export class CrisError extends Error {
                                    /**
                                     * Get this error type.
                                     */
                                    public readonly errorType : "CommunicationError"|"ValidationError"|"ExecutionError";
                                    /**
                                     * Gets the messages. At least one message is guranteed to exist.
                                     */
                                    public readonly messages: ReadonlyArray<SimpleUserMessage>; 
                                    /**
                                     * The Error.cause support is a mess. This replaces it at this level. 
                                     */
                                    public readonly innerError?: Error; 
                                    /**
                                     * When defined, enables to find the backend log entry.
                                     */
                                    public readonly logKey?: string; 
                                    /**
                                     * Gets the command that failed.
                                     */
                                    public readonly command: ICommand<unknown>;

                                    constructor( command: ICommand<unknown>, 
                                                 message: string, 
                                                 isValidationError: boolean,
                                                 innerError?: Error, 
                                                 messages?: ReadonlyArray<SimpleUserMessage>,
                                                 logKey?: string ) 
                                    {
                                        super( message );
                                        this.command = command;   
                                        this.errorType = isValidationError 
                                                            ? "ValidationError" 
                                                            : innerError ? "CommunicationError" : "ExecutionError";
                                        this.innerError = innerError;
                                        this.messages = messages && messages.length > 0 
                                                        ? messages
                                                        : [new SimpleUserMessage(UserMessageLevel.Error,message,0)];
                                        this.logKey = logKey;
                                    }
                                }
                                
                                """ );
                }
            }
        }

        // This one changes the folders.
        // The real one doesn't do this and injects code into the Poco TypeSript implementation.
        public class FakeTypeScriptCrisCommandGeneratorImplWithFolders : FakeTypeScriptCrisCommandGeneratorImpl
        {
            public override bool OnResolveObjectKey( IActivityMonitor monitor, TypeScriptContext context, TSTypeRequiredEventArgs e )
            {
                if( !base.OnResolveObjectKey( monitor, context, e ) ) return false;
                return true;
            }

            public override bool OnResolveType( IActivityMonitor monitor,
                                                TypeScriptContext context,
                                                TypeBuilderRequiredEventArgs builder )
            {
                if( !base.OnResolveType( monitor, context, builder ) ) return false;
                // All ICommand here (without specified TypeScript Folder) will be in Cris/Commands.
                // Their FileName will be without the "I" and prefixed by "CMD".
                // The real CommandDirectoryImpl does nothing here: ICommand are IPoco and
                // their folder/file organization is fine.
                if( typeof(IAbstractCommand).IsAssignableFrom( builder.Type ) )
                {
                    if( builder.SameFolderAs == null )
                    {
                        builder.Folder ??= builder.Type.Namespace!.Replace( '.', '/' );

                        const string autoMapping = "CK/StObj/TypeScript/Tests";
                        if( builder.Folder.StartsWith( autoMapping ) )
                        {
                            builder.Folder = string.Concat( "Commands/", builder.Folder.AsSpan( autoMapping.Length ) );
                        }
                    }
                    if( builder.FileName == null && builder.SameFileAs == null )
                    {
                        builder.FileName = string.Concat( "CMD", builder.Type.Name.AsSpan( 1 ), ".ts" );
                    }
                }
                return true;
            }

        }

        // This static class is only here to trigger the global FakeTypeScriptCrisCommandGeneratorImplWithFolders ITSCodeGenerator.
        // This is the same as the static class TypeScriptCrisCommandGenerator in CK.Cris.TypeScript package.
        [ContextBoundDelegation( "CK.StObj.TypeScript.Tests.CrisLike.CommandLikeTests+FakeTypeScriptCrisCommandGeneratorImplWithFolders, CK.StObj.TypeScript.Tests" )]
        public static class FakeTypeScriptCrisCommandGeneratorWithFolders
        {
        }

        // This static class is only here to trigger the global FakeTypeScriptCrisCommandGeneratorImpl ITSCodeGenerator.
        // This is the same as the static class TypeScriptCrisCommandGenerator in CK.Cris.TypeScript package.
        [ContextBoundDelegation( "CK.StObj.TypeScript.Tests.CrisLike.CommandLikeTests+FakeTypeScriptCrisCommandGeneratorImpl, CK.StObj.TypeScript.Tests" )]
        public static class FakeTypeScriptCrisCommandGenerator
        {
        }

        [Test]
        public void command_like_sample_with_interfaces()
        {
            var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();

            var tsTypes = new[]
            {
                // We must register the enum otherwise the [TypeScript} is ignored.
                typeof( Power ),
                typeof( ICommandOne ),
                typeof( ICommandTwo ),
                typeof( ICommandThree ),
                typeof( ICommandFour ),
                typeof( ICrisResult ),
                typeof( ICrisResultError )
            };
            TestHelper.GenerateTypeScript( targetProjectPath,
                                           tsTypes.Append( typeof( FakeCommandDirectory ) ).Append( typeof( FakeTypeScriptCrisCommandGeneratorWithFolders ) ),
                                           tsTypes );
            var p = targetProjectPath.Combine( "ck-gen/src" );
            File.ReadAllText( p.Combine( "TheFolder/Power.ts" ) ).Should().Contain( "export enum Power" );

            var tOne = File.ReadAllText( p.Combine( "TheFolder/CMDCommandOne.ts" ) );
            tOne.Should().Contain( """import { Power } from "./Power";""" )
                     .And.Contain( """import { CommandTwo } from "../Commands/CrisLike/CMDCommandTwo";""" );

            tOne.Should().Contain( "export class CommandOne implements ICommand {" )
                     .And.Contain( """public name: String = "",""" )
                     .And.Contain( """public power: Power = Power.None,""" )
                     .And.Contain( """public readonly friend: CommandTwo = new CommandTwo()""" );


            var tTwo = File.ReadAllText( p.Combine( "Commands/CrisLike/CMDCommandTwo.ts" ) );
            tTwo.Should().Contain( """import { Power } from "../../TheFolder/Power";""" )
                     .And.Contain( """import { CommandThree, CommandOne } from "../../TheFolder/CMDCommandOne";""" )
                     .And.Contain( """import { ICommand } from "../../CK/CrisLike/CMDCommand";""");

            tTwo.Should().Contain( "export class CommandTwo implements ICommand {" )
                     .And.Contain( "public age: Number = 0," )
                     .And.Contain( "public anotherPower: Power = Power.None," )
                     .And.Contain( "public friendThree: CommandThree = new CommandThree()," )
                     .And.Contain( "public anotherFriend?: CommandOne");

            var tFour = File.ReadAllText( p.Combine( "Commands/CrisLike/CMDCommandFour.ts" ) );
            tFour.Should().Contain( "export class CommandFour implements ICommand {" )
                     .And.Contain( "public readonly data: RecordData = new RecordData()," )
                     .And.Contain( "public uniqueId?: Guid" );

            var tRecord = File.ReadAllText( p.Combine( "CK/StObj/TypeScript/Tests/CrisLike/RecordData.ts" ) );
            tRecord.Should().Be( """
                /**
                 * Simple record data.
                 **/
                export class RecordData {
                constructor( 
                /**
                 * The data index.
                 **/
                public index: Number = 0, 
                /**
                 * A great name for the data.
                 **/
                public superName: String = ""
                ) {}
                }

                """ );
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
            var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();
            var tsTypes = new[]
            {
                typeof( IWithObjectCommand ),
                typeof( IWithObjectSpecializedAsStringCommand )
            };
            TestHelper.GenerateTypeScript( targetProjectPath,
                                           tsTypes.Append( typeof( FakeCommandDirectory ) ).Append( typeof( FakeTypeScriptCrisCommandGenerator ) ),
                                           tsTypes );
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
            var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();
            var tsTypes = new[]
            {
                typeof( IWithObjectCommand ),
                typeof( IWithObjectSpecializedAsPocoCommand ),
                typeof( IResult ),
                typeof( IWithObjectSpecializedAsSuperPocoCommand ),
                typeof( ISuperResult )
            };
            TestHelper.GenerateTypeScript( targetProjectPath,
                                           tsTypes.Append( typeof( FakeCommandDirectory ) ).Append( typeof( FakeTypeScriptCrisCommandGenerator ) ),
                                           tsTypes );
        }

    }
}
