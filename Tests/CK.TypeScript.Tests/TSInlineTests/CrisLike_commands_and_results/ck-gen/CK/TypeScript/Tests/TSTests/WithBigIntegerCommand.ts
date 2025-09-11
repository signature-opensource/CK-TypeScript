import { ICommandModel, ICommand } from '../../../Cris/Model';
import { CTSType } from '../../../Core/CTSType';

export class WithBigIntegerCommand implements ICommand {
public value: bigint;
public constructor()
public constructor(
value?: bigint)
constructor(
value?: bigint)
{
this.value = value ?? 0n;
CTSType["CK.TypeScript.Tests.TSTests.FullTSTests.IWithBigIntegerCommand"].set( this );
}

get commandModel(): ICommandModel { return WithBigIntegerCommand.#m; }

static #m =  {
applyUbiquitousValues( command: any, a: any, o: any ) {
/*Apply code comes HERE but FakeTypeScriptCrisCommandGeneratorImpl doesn't handle the ubiquitous values.*/
}

}
readonly _brand!: ICommand["_brand"] & {"20":any};
}
