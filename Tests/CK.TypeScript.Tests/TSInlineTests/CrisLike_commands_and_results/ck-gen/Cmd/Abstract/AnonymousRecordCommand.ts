import { ICommandModel, ICommand } from '../../CK/Cris/Model';
import { ICommandAbs } from './ICommandAbs';
import { Guid } from '../../System/Guid';
import { CTSType } from '../../CK/Core/CTSType';

/**
 * Concrete command where ICommandAbs works with an anonymous record.
 **/
export class AnonymousRecordCommand implements ICommand, ICommandAbs {
/**
 * An anonymous record key with field names. In TS it is {value: number, id: Guid}.
 * An object key.
 **/
public readonly key: {value: number, id: Guid};
/**
 * An anonymous record key without field names. In TS it is [number,Guid].
 **/
public readonly keyTuple: [number, Guid];
/**
 * An hybrid anonymous record key. In TS it is {item1: number, id: Guid}.
 **/
public readonly keyHybrid: {item1: number, id: Guid};
/**
 * The mutable list of anonymous record.
 * A list of key as objects.
 **/
public readonly keyList: Array<{value: number, id: Guid}>;
/**
 * The mutable set of anonymous record.
 * A set of key as objects.
 **/
public readonly keySet: Set<{value: number, id: Guid}>;
/**
 * The mutable dictionary of anonymous record.
 * A dictionary of key as objects.
 **/
public readonly keyDictionary: Map<string,{value: number, id: Guid}>;
public constructor()
public constructor(
key?: {value: number, id: Guid},
keyTuple?: [number, Guid],
keyHybrid?: {item1: number, id: Guid},
keyList?: Array<{value: number, id: Guid}>,
keySet?: Set<{value: number, id: Guid}>,
keyDictionary?: Map<string,{value: number, id: Guid}>)
constructor(
key?: {value: number, id: Guid},
keyTuple?: [number, Guid],
keyHybrid?: {item1: number, id: Guid},
keyList?: Array<{value: number, id: Guid}>,
keySet?: Set<{value: number, id: Guid}>,
keyDictionary?: Map<string,{value: number, id: Guid}>)
{
this.key = key ?? {value: 0, id: Guid.empty};
this.keyTuple = keyTuple ?? [0, Guid.empty];
this.keyHybrid = keyHybrid ?? {item1: 0, id: Guid.empty};
this.keyList = keyList ?? [];
this.keySet = keySet ?? new Set<{value: number, id: Guid}>();
this.keyDictionary = keyDictionary ?? new Map<string,{value: number, id: Guid}>();
CTSType["CK.TypeScript.Tests.CrisLike.IAnonymousRecordCommand"].set( this );
}

get commandModel(): ICommandModel { return AnonymousRecordCommand.#m; }

static #m =  {
applyUbiquitousValues( command: any, a: any, o: any ) {
/*Apply code comes HERE but FakeTypeScriptCrisCommandGeneratorImpl doesn't handle the ubiquitous values.*/
}

}
readonly _brand!: ICommand["_brand"] & ICommandAbs["_brand"] & {"3":any};
}
