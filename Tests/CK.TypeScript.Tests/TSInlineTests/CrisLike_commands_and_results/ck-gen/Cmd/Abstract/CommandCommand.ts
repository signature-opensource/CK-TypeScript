import { ICommandModel, ICommand } from '../../CK/Cris/Model';
import { ICommandAbsWithNullableKey } from './ICommandAbsWithNullableKey';
import { ExtendedCultureInfo } from '../../CK/Core/ExtendedCultureInfo';
import { CTSType } from '../../CK/Core/CTSType';

/**
 * Concrete command where ICommandAbsWithNullableKey works with any command.
 **/
export class CommandCommand implements ICommand, ICommandAbsWithNullableKey {
/**
 * The command key.
 * Here, the command must be nullable otherwise we would be
 * unable to have a default value for it. 
 * A nullable object key.
 **/
public key?: ICommand;
/**
 * The mutable list of command.
 * A list of key as objects.
 **/
public readonly keyList: Array<ICommand>;
/**
 * The mutable set of command is not possible: a set must have read-only compliant key and a poco is
 * everything but read-only compliant.
 * A set of key as objects.
 **/
public readonly keySet: Set<ExtendedCultureInfo>;
/**
 * The mutable dictionary of command.
 * A dictionary of key as objects.
 **/
public readonly keyDictionary: Map<string,ICommand>;
public constructor()
public constructor(
key?: ICommand,
keyList?: Array<ICommand>,
keySet?: Set<ExtendedCultureInfo>,
keyDictionary?: Map<string,ICommand>)
constructor(
key?: ICommand,
keyList?: Array<ICommand>,
keySet?: Set<ExtendedCultureInfo>,
keyDictionary?: Map<string,ICommand>)
{
this.key = key;
this.keyList = keyList ?? [];
this.keySet = keySet ?? new Set<ExtendedCultureInfo>();
this.keyDictionary = keyDictionary ?? new Map<string,ICommand>();
CTSType["CK.TypeScript.Tests.CrisLike.ICommandCommand"].set( this );
}

get commandModel(): ICommandModel { return CommandCommand.#m; }

static #m =  {
applyUbiquitousValues( command: any, a: any, o: any ) {
/*Apply code comes HERE but FakeTypeScriptCrisCommandGeneratorImpl doesn't handle the ubiquitous values.*/
}

}
readonly _brand!: ICommand["_brand"] & ICommandAbsWithNullableKey["_brand"] & {"4":any};
}
