import { ICommandAuthUnsafe } from './ICommandAuthUnsafe';

/**
 * Extends ICommandAuthUnsafe to ensure that the authentication level is 
 * ~~!:AuthLevel.Normal~~ (or 
 * ~~!:AuthLevel.Critical~~).
 **/
export interface ICommandAuthNormal extends ICommandAuthUnsafe {
readonly _brand: ICommandAuthUnsafe["_brand"] & {"28":any};
}
