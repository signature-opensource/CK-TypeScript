import { ICommandModel, ICommand } from '../../CK/Cris/Model';
import { ICommandAbs } from './ICommandAbs';
import { NamedRecord } from './NamedRecord';
import { CTSType } from '../../CK/Core/CTSType';

/**
 * Concrete command where ICommandAbs works with 
 * NamedRecord.
 **/
export class NamedRecordCommand implements ICommand, ICommandAbs {
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
public constructor()
public constructor(
key?: NamedRecord,
keyList?: Array<NamedRecord>,
keySet?: Set<NamedRecord>,
keyDictionary?: Map<string,NamedRecord>)
constructor(
key?: NamedRecord,
keyList?: Array<NamedRecord>,
keySet?: Set<NamedRecord>,
keyDictionary?: Map<string,NamedRecord>)
{
this.key = key ?? new NamedRecord();
this.keyList = keyList ?? [];
this.keySet = keySet ?? new Set<NamedRecord>();
this.keyDictionary = keyDictionary ?? new Map<string,NamedRecord>();
CTSType["CK.TypeScript.Tests.CrisLike.INamedRecordCommand"].set( this );
}

get commandModel(): ICommandModel { return NamedRecordCommand.#m; }

static #m =  {
applyUbiquitousValues( command: any, a: any, o: any ) {
/*Apply code comes HERE but FakeTypeScriptCrisCommandGeneratorImpl doesn't handle the ubiquitous values.*/
}

}
readonly _brand!: ICommand["_brand"] & ICommandAbs["_brand"] & {"6":any};
}
