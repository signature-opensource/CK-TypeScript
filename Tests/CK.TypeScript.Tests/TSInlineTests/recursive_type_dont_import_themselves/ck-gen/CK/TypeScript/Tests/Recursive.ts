import { IPoco } from '../../Core/IPoco';

export class Recursive implements IPoco {
public readonly children: Array<Recursive>;
public constructor()
public constructor(
children?: Array<Recursive>)
constructor(
children?: Array<Recursive>)
{
this.children = children ?? [];
}
readonly _brand!: IPoco["_brand"] & {"0":any};
}
