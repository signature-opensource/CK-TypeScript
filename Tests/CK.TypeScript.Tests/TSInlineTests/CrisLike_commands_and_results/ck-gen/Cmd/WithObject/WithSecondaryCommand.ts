import { ICommandModel, ICommand } from '../../CK/Cris/Model';
import { WithObjectCommand } from '../Some/WithObjectCommand';
import { CTSType } from '../../CK/Core/CTSType';

/**
 * Tests the SecondaryPoco use. SecondaryPoco is erased in TS: the primary IWithObjectCommand is used
 * everywhere a secondary definition appears.
 **/
export class WithSecondaryCommand implements ICommand {
/**
 * A mutable field. Uninitialized since nullable.
 **/
public nullableCmdWithSetter?: WithObjectCommand;
/**
 * A mutable field. Initialized to an initial command since non nullable.
 **/
public cmdWithSetter: WithObjectCommand;
/**
 * Read-only field. Initialized to an initial command.
 **/
public readonly cmdAuto: WithObjectCommand;
/**
 * Concrete list must have getter and setter.
 **/
public listSecondary: Array<WithObjectCommand>;
/**
 * "Standard" Poco covariant auto implementation.
 **/
public readonly listSecondaryAuto: Array<WithObjectCommand>;
public constructor()
public constructor(
nullableCmdWithSetter?: WithObjectCommand,
cmdWithSetter?: WithObjectCommand,
cmdAuto?: WithObjectCommand,
listSecondary?: Array<WithObjectCommand>,
listSecondaryAuto?: Array<WithObjectCommand>)
constructor(
nullableCmdWithSetter?: WithObjectCommand,
cmdWithSetter?: WithObjectCommand,
cmdAuto?: WithObjectCommand,
listSecondary?: Array<WithObjectCommand>,
listSecondaryAuto?: Array<WithObjectCommand>)
{
this.nullableCmdWithSetter = nullableCmdWithSetter;
this.cmdWithSetter = cmdWithSetter ?? new WithObjectCommand();
this.cmdAuto = cmdAuto ?? new WithObjectCommand();
this.listSecondary = listSecondary ?? [];
this.listSecondaryAuto = listSecondaryAuto ?? [];
CTSType["CK.TypeScript.Tests.CrisLike.IWithSecondaryCommand"].set( this );
}

get commandModel(): ICommandModel { return WithSecondaryCommand.#m; }

static #m =  {
applyUbiquitousValues( command: any, a: any, o: any ) {
/*Apply code comes HERE but FakeTypeScriptCrisCommandGeneratorImpl doesn't handle the ubiquitous values.*/
}

}
readonly _brand!: ICommand["_brand"] & {"18":any};
}
