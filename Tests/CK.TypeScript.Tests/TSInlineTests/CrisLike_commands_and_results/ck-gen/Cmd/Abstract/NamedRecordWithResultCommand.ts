import { ICommandModel, ICommand } from '../../CK/Cris/Model';
import { ICommandAbsWithResult } from './ICommandAbsWithResult';
import { NamedRecord } from './NamedRecord';
import { CTSType } from '../../CK/Core/CTSType';

/**
 * Concrete command where ICommandAbsWithResult works with 
 * NamedRecord.
 **/
export class NamedRecordWithResultCommand implements ICommand, ICommandAbsWithResult {
/**
 * The record.
 * An object key.
 **/
public readonly key: NamedRecord;
/**
 * The mutable list of record.
 * A list of key as objects.
 **/
public readonly keyList: Array<NamedRecord>;
/**
 * The mutable set of record.
 * A set of key as objects.
 **/
public readonly keySet: Set<NamedRecord>;
/**
 * The mutable dictionary of record.
 * A dictionary of key as objects.
 **/
public readonly keyDictionary: Map<string,NamedRecord>;
/**
 * The mutable list of record.
 **/
public readonly keyListOfNullable: Array<NamedRecord|undefined>;
public constructor()
public constructor(
key?: NamedRecord,
keyList?: Array<NamedRecord>,
keySet?: Set<NamedRecord>,
keyDictionary?: Map<string,NamedRecord>,
keyListOfNullable?: Array<NamedRecord|undefined>)
constructor(
key?: NamedRecord,
keyList?: Array<NamedRecord>,
keySet?: Set<NamedRecord>,
keyDictionary?: Map<string,NamedRecord>,
keyListOfNullable?: Array<NamedRecord|undefined>)
{
this.key = key ?? new NamedRecord();
this.keyList = keyList ?? [];
this.keySet = keySet ?? new Set<NamedRecord>();
this.keyDictionary = keyDictionary ?? new Map<string,NamedRecord>();
this.keyListOfNullable = keyListOfNullable ?? [];
CTSType["CK.TypeScript.Tests.CrisLike.INamedRecordWithResultCommand"].set( this );
}

get commandModel(): ICommandModel { return NamedRecordWithResultCommand.#m; }

static #m =  {
applyUbiquitousValues( command: any, a: any, o: any ) {
/*Apply code comes HERE but FakeTypeScriptCrisCommandGeneratorImpl doesn't handle the ubiquitous values.*/
}

}
readonly _brand!: ICommand["_brand"] & ICommandAbsWithResult["_brand"] & {"7":any};
}
