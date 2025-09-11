import { ICommandAuthNormal } from './ICommandAuthNormal';

/**
 * Extends ICommandAuthNormal to ensure that the authentication level is 
 * ~~!:AuthLevel.Critical~~.
 **/
export interface ICommandAuthCritical extends ICommandAuthNormal {
readonly _brand: ICommandAuthNormal["_brand"] & {"27":any};
}
