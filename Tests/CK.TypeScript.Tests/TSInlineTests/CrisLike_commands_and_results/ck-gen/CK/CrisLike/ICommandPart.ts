import { IAbstractCommand } from '../Cris/Model';

/**
 * Marker interface to define mixable command parts.
 * 
 * Since command parts de facto defines a command object, their name should start 
 * with "ICommand" in order to distinguish them from actual commands that
 * should be suffixed with "Command".
 * 
 * Parts can also be extended: when defining a specialized part that extends an
 * existing ICommandPart, the 
 * CKTypeDefinerAttribute must be
 * applied to the specialized part.
 **/
export interface ICommandPart extends IAbstractCommand {
readonly _brand: IAbstractCommand["_brand"] & {"31":any};
}
