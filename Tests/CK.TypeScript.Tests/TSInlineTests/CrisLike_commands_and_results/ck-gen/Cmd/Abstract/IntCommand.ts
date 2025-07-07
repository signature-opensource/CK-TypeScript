import { ICommandModel, ICommand } from '../../CK/Cris/Model';
import { ICommandAbs } from './ICommandAbs';
import { CTSType } from '../../CK/Core/CTSType';

/**
 * Concrete command where ICommandAbs works with integers.
 **/
export class IntCommand implements ICommand, ICommandAbs {
/**
 * The integer key.
 * An object key.
 **/
public key: number;
/**
 * The mutable list of integer.
 * A list of key as objects.
 **/
public readonly keyList: Array<number>;
/**
 * The mutable set of integer.
 * A set of key as objects.
 **/
public readonly keySet: Set<number>;
/**
 * The mutable dictionary of integer.
 * A dictionary of key as objects.
 **/
public readonly keyDictionary: Map<string,number>;
public constructor()
public constructor(
key?: number,
keyList?: Array<number>,
keySet?: Set<number>,
keyDictionary?: Map<string,number>)
constructor(
key?: number,
keyList?: Array<number>,
keySet?: Set<number>,
keyDictionary?: Map<string,number>)
{
this.key = key ?? 0;
this.keyList = keyList ?? [];
this.keySet = keySet ?? new Set<number>();
this.keyDictionary = keyDictionary ?? new Map<string,number>();
CTSType["CK.TypeScript.Tests.CrisLike.IIntCommand"].set( this );
}

get commandModel(): ICommandModel { return IntCommand.#m; }

static #m =  {
applyUbiquitousValues( command: any, a: any, o: any ) {
/*Apply code comes HERE but FakeTypeScriptCrisCommandGeneratorImpl doesn't handle the ubiquitous values.*/
}

}
readonly _brand!: ICommand["_brand"] & ICommandAbs["_brand"] & {"5":any};
}
