import { ICommandPart } from '../../CK/CrisLike/ICommandPart';

/**
 * ICommandAbs is a part with an object key and
 * a list, a set and a dictionary of objects.
 * 
 * The properties here can only be "Abstract Read Only" properties, if they were
 * "concrete properties", they would impose the type.
 **/
export interface ICommandAbs extends ICommandPart {
/**
 * Gets an object key.
 **/
readonly key: {};
/**
 * Gets a list of key as objects.
 **/
readonly keyList: ReadonlyArray<{}>;
/**
 * Gets a set of key as objects.
 **/
readonly keySet: ReadonlySet<{}>;
/**
 * Gets a dictionary of key as objects.
 **/
readonly keyDictionary: ReadonlyMap<string,{}>;
readonly _brand: ICommandPart["_brand"] & {"25":any};
}
