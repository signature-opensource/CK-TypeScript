import { Decimal } from 'decimal.js-light';
import { ICommandModel, ICommand } from '../../../Cris/Model';
import { CTSType } from '../../../Core/CTSType';

export class WithDecimalCommand implements ICommand {
public value: Decimal;
public constructor()
public constructor(
value?: Decimal)
constructor(
value?: Decimal)
{
this.value = value ?? new Decimal(0);
CTSType["CK.TypeScript.Tests.TSTests.FullTSTests.IWithDecimalCommand"].set( this );
}

get commandModel(): ICommandModel { return WithDecimalCommand.#m; }

static #m =  {
applyUbiquitousValues( command: any, a: any, o: any ) {
/*Apply code comes HERE but FakeTypeScriptCrisCommandGeneratorImpl doesn't handle the ubiquitous values.*/
}

}
readonly _brand!: ICommand["_brand"] & {"21":any};
}
