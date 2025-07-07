import { ICommandModel, ICommand } from '../../CK/Cris/Model';
import { ICommandAbs } from './ICommandAbs';
import { CTSType } from '../../CK/Core/CTSType';

/**
 * Concrete command where ICommandAbs works with strings.
 **/
export class StringCommand implements ICommand, ICommandAbs {
/**
 * The string key.
 * An object key.
 **/
public key: string;
/**
 * The mutable list of string.
 * A list of key as objects.
 **/
public readonly keyList: Array<string>;
/**
 * The mutable set of integer.
 * A set of key as objects.
 **/
public readonly keySet: Set<string>;
/**
 * The mutable dictionary of integer.
 * A dictionary of key as objects.
 **/
public readonly keyDictionary: Map<string,string>;
public constructor()
public constructor(
key?: string,
keyList?: Array<string>,
keySet?: Set<string>,
keyDictionary?: Map<string,string>)
constructor(
key?: string,
keyList?: Array<string>,
keySet?: Set<string>,
keyDictionary?: Map<string,string>)
{
this.key = key ?? "";
this.keyList = keyList ?? [];
this.keySet = keySet ?? new Set<string>();
this.keyDictionary = keyDictionary ?? new Map<string,string>();
CTSType["CK.TypeScript.Tests.CrisLike.IStringCommand"].set( this );
}

get commandModel(): ICommandModel { return StringCommand.#m; }

static #m =  {
applyUbiquitousValues( command: any, a: any, o: any ) {
/*Apply code comes HERE but FakeTypeScriptCrisCommandGeneratorImpl doesn't handle the ubiquitous values.*/
}

}
readonly _brand!: ICommand["_brand"] & ICommandAbs["_brand"] & {"14":any};
}
