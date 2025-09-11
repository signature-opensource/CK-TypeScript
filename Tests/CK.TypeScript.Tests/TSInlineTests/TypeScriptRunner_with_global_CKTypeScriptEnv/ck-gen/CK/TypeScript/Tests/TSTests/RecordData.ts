import { DateTime } from 'luxon';

export class RecordData {
public constructor(
public time: DateTime = DateTime.utc(1,1,1,0,0,0,0), 
public names: Array<string> = [])
{
}
readonly _brand!: {"17":any};
}
