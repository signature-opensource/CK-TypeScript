using CK.CrisLike;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
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

            /// <summary>
            /// Gets or sets a command that returns a string.
            /// Since NO such command exist, 
            /// </summary>
            ICommand<string>? ThereIsNoSuchCommand { get; set; }
        }

        [Test]
        public void command_like_sample_with_interfaces()
        {
            var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();

            var tsTypes = new[]
            {
                // We must register the enum otherwise the [TypeScript] is ignored.
                typeof( Power ),
                typeof( ICommandOne ),
                typeof( ICommandTwo ),
                typeof( ICommandThree ),
                typeof( ICommandFour )
            };
            TestHelper.GenerateTypeScript( targetProjectPath,
                                           // Registers Results only as Poco type: it is the FakeTypeScriptCrisCommandGeneratorImpl
                                           // that ensures that they belong to the TypeScriptSet.
                                           registeredTypes: tsTypes.Concat( new[] { typeof( IAspNetCrisResult ),
                                                                                    typeof( IAspNetCrisResultError ),
                                                                                    typeof( FakeTypeScriptCrisCommandGeneratorWithFolders ) } ),
                                           tsTypes );
            var p = targetProjectPath.Combine( "ck-gen/src" );
            File.ReadAllText( p.Combine( "TheFolder/Power.ts" ) ).Should().Contain( "export enum Power" );

            var tOne = File.ReadAllText( p.Combine( "TheFolder/CMDCommandOne.ts" ) ).ReplaceLineEndings();
            tOne.Should().Contain( """import { Power } from "./Power";""" )
                     .And.Contain( """import { CommandTwo } from "../Commands/CrisLike/CMDCommandTwo";""" );

            tOne.Should().Contain( "export class CommandOne implements ICommand {" )
                     .And.Contain( """
                     public constructor()
                     public constructor(
                     name: String,
                     power: Power,
                     friend: CommandTwo)
                     constructor(
                     name?: String,
                     power?: Power,
                     friend?: CommandTwo)
                     {
                     this.name = name ?? "";
                     this.power = power ?? Power.None;
                     this.friend = friend ?? new CommandTwo();
                     }
                     """.ReplaceLineEndings() );


            var tTwo = File.ReadAllText( p.Combine( "Commands/CrisLike/CMDCommandTwo.ts" ) ).ReplaceLineEndings();
            tTwo.Should().Contain( """import { Power } from "../../TheFolder/Power";""" )
                     .And.Contain( """import { CommandOne, CommandThree } from "../../TheFolder/CMDCommandOne";""" )
                     .And.Contain( """import { ICommandModel, ICommand } from "../../CK/Cris/Model";""" );

            tTwo.Should().Contain( "export class CommandTwo implements ICommand {" )
                     .And.Contain( """
                    public constructor()
                    public constructor(
                    age: Number,
                    anotherPower: Power,
                    anotherFriend: CommandOne|undefined,
                    friendThree: CommandThree)
                    constructor(
                    age?: Number,
                    anotherPower?: Power,
                    anotherFriend?: CommandOne,
                    friendThree?: CommandThree)
                    {
                    this.age = age ?? 0;
                    this.anotherPower = anotherPower ?? Power.None;
                    this.anotherFriend = anotherFriend;
                    this.friendThree = friendThree ?? new CommandThree();
                    }
                    """.ReplaceLineEndings() );

            var tFour = File.ReadAllText( p.Combine( "Commands/CrisLike/CMDCommandFour.ts" ) ).ReplaceLineEndings();
            tFour.Should().Contain( "export class CommandFour implements ICommand {" )
                     .And.Contain( """
                    public constructor()
                    public constructor(
                    uniqueId: Guid|undefined,
                    data: RecordData)
                    constructor(
                    uniqueId?: Guid,
                    data?: RecordData)
                    {
                    this.uniqueId = uniqueId;
                    this.data = data ?? new RecordData();
                    }
                    """.ReplaceLineEndings() );

            var tRecord = File.ReadAllText( p.Combine( "CK/StObj/TypeScript/Tests/CrisLike/RecordData.ts" ) ).ReplaceLineEndings();
            tRecord.Should().Be( """
                /**
                 * Simple record data.
                 **/
                export class RecordData {
                public constructor(
                /**
                 * The data index.
                 **/
                public index: Number = 0, 
                /**
                 * A great name for the data.
                 **/
                public superName: String = "")
                {
                }
                readonly _brand!: {"15":any};
                }

                """.ReplaceLineEndings() );
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
                                           // Registers Results only as Poco type: it is the FakeTypeScriptCrisCommandGeneratorImpl
                                           // that ensures that they belong to the TypeScriptSet.
                                           registeredTypes: tsTypes.Concat( new[] { typeof( IAspNetCrisResult ),
                                                                                    typeof( IAspNetCrisResultError ),
                                                                                    typeof( FakeTypeScriptCrisCommandGeneratorWithFolders ) } ),
                                           tsTypes );
        }

        [Test]
        public void command_with_poco_results_specialized_and_parts()
        {
            var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();
            var tsTypes = new[]
            {
                typeof( ISomeCommand ),
                typeof( ISomeIsCriticalAndReturnsIntCommand ),
                typeof( IWithObjectCommand ),
                typeof( IWithObjectSpecializedAsPocoCommand ),
                typeof( IResult ),
                typeof( IWithObjectSpecializedAsSuperPocoCommand ),
                typeof( ISuperResult )
            };
            TestHelper.GenerateTypeScript( targetProjectPath,
                                           // Registers Results only as Poco type: it is the FakeTypeScriptCrisCommandGeneratorImpl
                                           // that ensures that they belong to the TypeScriptSet.
                                           registeredTypes: tsTypes.Concat( new[] { typeof( IAspNetCrisResult ),
                                                                                    typeof( IAspNetCrisResultError ),
                                                                                    typeof( FakeTypeScriptCrisCommandGeneratorWithFolders ) } ),
                                           tsTypes );
        }

        [Test]
        public void commands_with_string_and_command()
        {
            var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();

            var tsTypes = new[] { typeof( IStringCommand ), typeof( ICommandCommand ) };
            TestHelper.GenerateTypeScript( targetProjectPath,
                                           // Registers Results only as Poco type: it is the FakeTypeScriptCrisCommandGeneratorImpl
                                           // that ensures that they belong to the TypeScriptSet.
                                           registeredTypes: tsTypes.Concat( new[] { typeof( IAspNetCrisResult ),
                                                                                    typeof( IAspNetCrisResultError ),
                                                                                    typeof( FakeTypeScriptCrisCommandGenerator ) } ),
                                           tsTypes );
            var p = targetProjectPath.Combine( "ck-gen/src" );

            var tS = File.ReadAllText( p.Combine( "CK/StObj/TypeScript/Tests/CrisLike/StringCommand.ts" ) ).ReplaceLineEndings();
            tS.Should().StartWith( """
                import { ICommandModel, ICommand } from "../../../../Cris/Model";
                import { ICommandAbs } from "./ICommandAbs";
                """.ReplaceLineEndings() );
            tS.Should().Contain( "export class StringCommand implements ICommand, ICommandAbs {" )
                    .And.Contain( """
                constructor(
                key?: String,
                keyList?: Array<String>,
                keySet?: Set<String>,
                keyDictionary?: Map<String,String>)
                {
                this.key = key ?? "";
                this.keyList = keyList ?? [];
                this.keySet = keySet ?? new Set<String>();
                this.keyDictionary = keyDictionary ?? new Map<String,String>();
                }
                """.ReplaceLineEndings() );

            var tC = File.ReadAllText( p.Combine( "CK/StObj/TypeScript/Tests/CrisLike/CommandCommand.ts" ) ).ReplaceLineEndings();
            tC.Should().StartWith( """
                import { ICommandModel, ICommand } from "../../../../Cris/Model";
                import { ICommandAbsWithNullableKey } from "./ICommandAbsWithNullableKey";
                import { ExtendedCultureInfo } from "../../../../Core/ExtendedCultureInfo";
                """.ReplaceLineEndings() );
            tC.Should().Contain( "export class CommandCommand implements ICommand, ICommandAbsWithNullableKey {" )
                    .And.Contain( """
                constructor(
                key?: ICommand,
                keyList?: Array<ICommand>,
                keySet?: Set<ExtendedCultureInfo>,
                keyDictionary?: Map<String,ICommand>)
                {
                this.key = key;
                this.keyList = keyList ?? [];
                this.keySet = keySet ?? new Set<ExtendedCultureInfo>();
                this.keyDictionary = keyDictionary ?? new Map<String,ICommand>();
                }
                """.ReplaceLineEndings() );

        }
    }
}
