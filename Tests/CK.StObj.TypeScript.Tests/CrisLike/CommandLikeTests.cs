using CK.Core;
using CK.CrisLike;
using CK.Setup;
using CK.TypeScript.CodeGen;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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
                // We must register the enum otherwise the [TypeScript} is ignored.
                typeof( Power ),
                typeof( ICommandOne ),
                typeof( ICommandTwo ),
                typeof( ICommandThree ),
                typeof( ICommandFour )
            };
            TestHelper.GenerateTypeScript( targetProjectPath,
                                           // Registers Results only as Poco type: it is the FakeTypeScriptCrisCommandGeneratorImpl
                                           // that ensures that they benlong to the TypeScriptSet.
                                           registeredTypes: tsTypes.Concat( new[] { typeof( IAspNetCrisResult ),
                                                                                    typeof( IAspNetCrisResultError ),
                                                                                    typeof( FakeTypeScriptCrisCommandGeneratorWithFolders ) } ),
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
                     .And.Contain( """import { ICommandModel, ICommand } from "../../CK/Cris/Model";""" );

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

                """ );
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
                                           // that ensures that they benlong to the TypeScriptSet.
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
                typeof( ISomeCommandIsCriticalAndReturnsInt ),
                typeof( IWithObjectCommand ),
                typeof( IWithObjectSpecializedAsPocoCommand ),
                typeof( IResult ),
                typeof( IWithObjectSpecializedAsSuperPocoCommand ),
                typeof( ISuperResult )
            };
            TestHelper.GenerateTypeScript( targetProjectPath,
                                           // Registers Results only as Poco type: it is the FakeTypeScriptCrisCommandGeneratorImpl
                                           // that ensures that they benlong to the TypeScriptSet.
                                           registeredTypes: tsTypes.Concat( new[] { typeof( IAspNetCrisResult ),
                                                                                    typeof( IAspNetCrisResultError ),
                                                                                    typeof( FakeTypeScriptCrisCommandGeneratorWithFolders ) } ),
                                           tsTypes );
        }

    }
}
