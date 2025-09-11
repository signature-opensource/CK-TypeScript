import { IPoco } from '../../../Core/IPoco';
import { WithUnions } from './WithUnions';

/**
 * Demonstrates the read only properties support.
 **/
export class WithReadOnly implements IPoco {
/**
 * The required target path.
 **/
public targetPath: string;
/**
 * The power.
 **/
public power?: number;
/**
 * The mutable list of string values.
 **/
public readonly list: Array<string>;
/**
 * The mutable map from name to numeric values.
 **/
public readonly map: Map<string,number|undefined>;
/**
 * The mutable set of unique string.
 **/
public readonly set: Set<string>;
/**
 * The union types poco.
 **/
public readonly poco: WithUnions;
public constructor()
public constructor(
targetPath?: string,
power?: number,
list?: Array<string>,
map?: Map<string,number|undefined>,
set?: Set<string>,
poco?: WithUnions)
constructor(
targetPath?: string,
power?: number,
list?: Array<string>,
map?: Map<string,number|undefined>,
set?: Set<string>,
poco?: WithUnions)
{
this.targetPath = targetPath ?? "The/Default/Path";
this.power = power;
this.list = list ?? [];
this.map = map ?? new Map<string,number|undefined>();
this.set = set ?? new Set<string>();
this.poco = poco ?? new WithUnions();
}
readonly _brand!: IPoco["_brand"] & {"0":any};
}
