import { ICommandPart } from '../../CK/CrisLike/ICommandPart';

/**
 * ICommandAbs has a non nullable object key: this can
 * be used only with types that has a devault value. An Abstract IPoco has no
 * default value: to reference an Abstract IPoco, the property MUST be nullable.
 * 
 * This is not the case for the collections since non null objects can be added and
 * the empty collection is the default value.
 **/
export interface ICommandAbsWithNullableKey extends ICommandPart {
/**
 * Gets a nullable object key.
 **/
readonly key?: {};
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
readonly _brand: ICommandPart["_brand"] & {"26":any};
}
