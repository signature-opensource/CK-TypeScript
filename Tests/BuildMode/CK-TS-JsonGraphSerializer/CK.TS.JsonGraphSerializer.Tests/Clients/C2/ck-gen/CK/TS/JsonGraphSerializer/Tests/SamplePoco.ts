import { IPoco } from '../../../Core/IPoco';
import { CTSType } from '../../../Core/CTSType';

export class SamplePoco implements IPoco {
public data: string;
public value: number;
public constructor()
public constructor(
data?: string,
value?: number)
constructor(
data?: string,
value?: number)
{
this.data = data ?? "";
this.value = value ?? 0;
CTSType["CK.TS.JsonGraphSerializer.Tests.MultipleTypeScriptTests.ISamplePoco"].set( this );
}
readonly _brand!: IPoco["_brand"] & {"0":any};
}
