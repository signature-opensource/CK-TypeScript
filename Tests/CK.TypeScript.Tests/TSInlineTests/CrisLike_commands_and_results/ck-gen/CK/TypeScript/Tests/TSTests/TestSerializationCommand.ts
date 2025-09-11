import { DateTime, Duration } from 'luxon';
import { Decimal } from 'decimal.js-light';
import { ICommandModel, ICommand } from '../../../Cris/Model';
import { GrantLevel } from '../../../Core/GrantLevel';
import { TypeKind } from '../../../../Microsoft/CodeAnalysis/TypeKind';
import { Guid } from '../../../../System/Guid';
import { SimpleUserMessage } from '../../../Core/SimpleUserMessage';
import { CTSType } from '../../../Core/CTSType';

export class TestSerializationCommand implements ICommand {
public string: string;
public int32: number;
public single: number;
public double: number;
public long: bigint;
public uLong: bigint;
public bigInteger: bigint;
public grantLevel: GrantLevel;
public typeKind: TypeKind;
public guid: Guid;
public dateTime: DateTime;
public timeSpan: Duration;
public simpleUserMessage?: SimpleUserMessage;
public decimal: Decimal;
public constructor()
public constructor(
string?: string,
int32?: number,
single?: number,
double?: number,
long?: bigint,
uLong?: bigint,
bigInteger?: bigint,
grantLevel?: GrantLevel,
typeKind?: TypeKind,
guid?: Guid,
dateTime?: DateTime,
timeSpan?: Duration,
simpleUserMessage?: SimpleUserMessage,
decimal?: Decimal)
constructor(
string?: string,
int32?: number,
single?: number,
double?: number,
long?: bigint,
uLong?: bigint,
bigInteger?: bigint,
grantLevel?: GrantLevel,
typeKind?: TypeKind,
guid?: Guid,
dateTime?: DateTime,
timeSpan?: Duration,
simpleUserMessage?: SimpleUserMessage,
decimal?: Decimal)
{
this.string = string ?? "";
this.int32 = int32 ?? 0;
this.single = single ?? 0;
this.double = double ?? 0;
this.long = long ?? 0n;
this.uLong = uLong ?? 0n;
this.bigInteger = bigInteger ?? 0n;
this.grantLevel = grantLevel ?? GrantLevel.Blind;
this.typeKind = typeKind ?? TypeKind.Unknown;
this.guid = guid ?? Guid.empty;
this.dateTime = dateTime ?? DateTime.utc(1,1,1,0,0,0,0);
this.timeSpan = timeSpan ?? Duration.fromMillis(0);
this.simpleUserMessage = simpleUserMessage;
this.decimal = decimal ?? new Decimal(0);
CTSType["CK.TypeScript.Tests.TSTests.FullTSTests.ITestSerializationCommand"].set( this );
}

get commandModel(): ICommandModel { return TestSerializationCommand.#m; }

static #m =  {
applyUbiquitousValues( command: any, a: any, o: any ) {
/*Apply code comes HERE but FakeTypeScriptCrisCommandGeneratorImpl doesn't handle the ubiquitous values.*/
}

}
readonly _brand!: ICommand["_brand"] & {"19":any};
}
