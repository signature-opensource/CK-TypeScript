import { ICommandAbs } from './ICommandAbs';
import { ICommand } from '../../CK/Cris/Model';

/**
 * Specializes the ICommandAbs to return an object.
 **/
export interface ICommandAbsWithResult extends ICommandAbs, ICommand<{}|undefined> {
readonly _brand: ICommandAbs["_brand"] & ICommand<{}|undefined>["_brand"] & {"24":any};
}
