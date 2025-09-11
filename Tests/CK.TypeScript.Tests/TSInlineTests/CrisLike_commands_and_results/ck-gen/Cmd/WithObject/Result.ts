import { IPoco } from '../../CK/Core/IPoco';
import { CTSType } from '../../CK/Core/CTSType';

/**
 * A result object with an integer.
 * Secondary definition that adds a string to IResult.
 **/
export class Result implements IPoco {
/**
 * The result value.
 **/
public result: number;
/**
 * A string result.
 **/
public superResult: string;
public constructor()
public constructor(
result?: number,
superResult?: string)
constructor(
result?: number,
superResult?: string)
{
this.result = result ?? 0;
this.superResult = superResult ?? "";
CTSType["CK.TypeScript.Tests.CrisLike.IResult"].set( this );
}
readonly _brand!: IPoco["_brand"] & {"8":any};
}
