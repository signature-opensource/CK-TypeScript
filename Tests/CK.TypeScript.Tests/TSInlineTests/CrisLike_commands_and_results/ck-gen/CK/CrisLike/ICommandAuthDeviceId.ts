import { ICommandAuthUnsafe } from './ICommandAuthUnsafe';

/**
 * Extends the basic ICommandAuthUnsafe to add the 
 * ICommandAuthDeviceId.deviceId field.
 **/
export interface ICommandAuthDeviceId extends ICommandAuthUnsafe {
/**
 * Gets or sets the device identifier.
 **/
deviceId: string;
readonly _brand: ICommandAuthUnsafe["_brand"] & {"29":any};
}
