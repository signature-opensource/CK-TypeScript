import { ICommandModel, ICommand } from '../../CK/Cris/Model';
import { CTSType } from '../../CK/Core/CTSType';

/**
 * Simple command.
 **/
export class SimpleCommand implements ICommand {
/**
 * The action.
 **/
public action?: string;
public constructor()
public constructor(
action?: string)
constructor(
action?: string)
{
this.action = action;
CTSType["CK.TypeScript.Tests.CrisLike.ISimpleCommand"].set( this );
}

get commandModel(): ICommandModel { return SimpleCommand.#m; }

static #m =  {
applyUbiquitousValues( command: any, a: any, o: any ) {
/*Apply code comes HERE but FakeTypeScriptCrisCommandGeneratorImpl doesn't handle the ubiquitous values.*/
}

}
readonly _brand!: ICommand["_brand"] & {"10":any};
}
