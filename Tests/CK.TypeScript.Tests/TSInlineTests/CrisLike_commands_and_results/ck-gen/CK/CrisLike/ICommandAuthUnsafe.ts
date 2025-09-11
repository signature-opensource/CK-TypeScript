import { ICommandPart } from './ICommandPart';

/**
 * Defines the ICommandAuthUnsafe.actorId field.
 * This is the most basic command part: the only guaranty is that the actor identifier is the
 * current ~~!:IAuthenticationInfo.UnsafeUser~~.
 **/
export interface ICommandAuthUnsafe extends ICommandPart {
/**
 * Gets or sets the actor identifier.
 * The default ~~!:CrisAuthenticationService~~ validates this field against the
 * current ~~!:IAuthenticationInfo.UnsafeUser~~.
 **/
actorId: number;
readonly _brand: ICommandPart["_brand"] & {"30":any};
}
