import { ICommandModel, ICommand } from '../../CK/Cris/Model';
import { ICommandAuthCritical } from '../../CK/CrisLike/ICommandAuthCritical';
import { Guid } from '../../System/Guid';
import { CTSType } from '../../CK/Core/CTSType';

/**
 * Some command requires a regular authentication level.
 * Secondary definition that makes SomeCommand require a critical authentication level and return
 * a integer.
 **/
export class SomeCommand implements ICommand, ICommand<number>, ICommandAuthCritical {
/**
 * The action identifier.
 **/
public actionId: Guid;
/**
 * The actor identifier.
 * The default ~~!:CrisAuthenticationService~~ validates this field against the
 * current ~~!:IAuthenticationInfo.UnsafeUser~~.
 **/
public actorId: number;
public constructor()
public constructor(
actionId?: Guid,
actorId?: number)
constructor(
actionId?: Guid,
actorId?: number)
{
this.actionId = actionId ?? Guid.empty;
this.actorId = actorId ?? 0;
CTSType["CK.TypeScript.Tests.CrisLike.ISomeCommand"].set( this );
}

get commandModel(): ICommandModel { return SomeCommand.#m; }

static #m =  {
applyUbiquitousValues( command: any, a: any, o: any ) {
/*Apply code comes HERE but FakeTypeScriptCrisCommandGeneratorImpl doesn't handle the ubiquitous values.*/
}

}
readonly _brand!: ICommand["_brand"] & ICommand<number>["_brand"] & ICommandAuthCritical["_brand"] & {"12":any};
}
