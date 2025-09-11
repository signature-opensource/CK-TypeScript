import { IPoco } from '../../../Core/IPoco';
import { RecordData } from './RecordData';
import { WithUnions } from './WithUnions';
import { Guid } from '../../../../System/Guid';
import { WithReadOnly } from './WithReadOnly';

export class WithTyped implements IPoco {
public readonly typedList: Array<RecordData>;
public readonly typedDic1: Map<string,WithUnions>;
public readonly typedDic2: Map<Guid,WithReadOnly|undefined>;
public constructor()
public constructor(
typedList?: Array<RecordData>,
typedDic1?: Map<string,WithUnions>,
typedDic2?: Map<Guid,WithReadOnly|undefined>)
constructor(
typedList?: Array<RecordData>,
typedDic1?: Map<string,WithUnions>,
typedDic2?: Map<Guid,WithReadOnly|undefined>)
{
this.typedList = typedList ?? [];
this.typedDic1 = typedDic1 ?? new Map<string,WithUnions>();
this.typedDic2 = typedDic2 ?? new Map<Guid,WithReadOnly|undefined>();
}
readonly _brand!: IPoco["_brand"] & {"1":any};
}
