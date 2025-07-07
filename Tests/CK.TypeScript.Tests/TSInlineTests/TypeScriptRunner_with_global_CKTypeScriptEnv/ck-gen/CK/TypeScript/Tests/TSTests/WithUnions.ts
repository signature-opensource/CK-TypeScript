import { IPoco } from '../../../Core/IPoco';

export class WithUnions implements IPoco {
/**
 * A nullable int or string.
 **/
public nullableIntOrString?: number|string;
/**
 * A non nullable int or string.
 **/
public intOrStringNoDefault: number|string;
/**
 * A non nullable int or string with default.
 **/
public intOrStringWithDefault: number|string;
/**
 * A non nullable int or string with default.
 **/
public nullableIntOrStringWithDefault: number|string|undefined;
public constructor()
public constructor(
nullableIntOrString?: number|string,
intOrStringNoDefault?: number|string,
intOrStringWithDefault?: number|string,
nullableIntOrStringWithDefault?: number|string)
constructor(
nullableIntOrString?: number|string,
intOrStringNoDefault?: number|string,
intOrStringWithDefault?: number|string,
nullableIntOrStringWithDefault?: number|string)
{
this.nullableIntOrString = nullableIntOrString;
this.intOrStringNoDefault = intOrStringNoDefault ?? 0;
this.intOrStringWithDefault = intOrStringWithDefault ?? 3712;
this.nullableIntOrStringWithDefault = nullableIntOrStringWithDefault ?? 42;
}
readonly _brand!: IPoco["_brand"] & {"2":any};
}
