import { ICommandModel, ICommand } from '../../CK/Cris/Model';
import { CTSType } from '../../CK/Core/CTSType';

/**
 * Simplest possible command: it is empty.
 **/
export class SimplestCommand implements ICommand {
public constructor()
public constructor()
constructor()
{
CTSType["CK.TypeScript.Tests.CrisLike.ISimplestCommand"].set( this );
}

get commandModel(): ICommandModel { return SimplestCommand.#m; }

static #m =  {
applyUbiquitousValues( command: any, a: any, o: any ) {
/*Apply code comes HERE but FakeTypeScriptCrisCommandGeneratorImpl doesn't handle the ubiquitous values.*/
}

}
readonly _brand!: ICommand["_brand"] & {"11":any};
}
