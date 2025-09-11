import { DateTime, Duration } from 'luxon';
import { Decimal } from 'decimal.js-light';
import { SomeCommand } from '../../Cmd/Some/SomeCommand';
import { SimpleCommand } from '../../Cmd/Some/SimpleCommand';
import { SimplestCommand } from '../../Cmd/Some/SimplestCommand';
import { WithObjectCommand } from '../../Cmd/Some/WithObjectCommand';
import { Result } from '../../Cmd/WithObject/Result';
import { WithSecondaryCommand } from '../../Cmd/WithObject/WithSecondaryCommand';
import { IntCommand } from '../../Cmd/Abstract/IntCommand';
import { StringCommand } from '../../Cmd/Abstract/StringCommand';
import { NamedRecord } from '../../Cmd/Abstract/NamedRecord';
import { NamedRecordCommand } from '../../Cmd/Abstract/NamedRecordCommand';
import { AnonymousRecordCommand } from '../../Cmd/Abstract/AnonymousRecordCommand';
import { CommandCommand } from '../../Cmd/Abstract/CommandCommand';
import { NamedRecordWithResultCommand } from '../../Cmd/Abstract/NamedRecordWithResultCommand';
import { TestSerializationCommand } from '../TypeScript/Tests/TSTests/TestSerializationCommand';
import { WithDecimalCommand } from '../TypeScript/Tests/TSTests/WithDecimalCommand';
import { WithULongCommand } from '../TypeScript/Tests/TSTests/WithULongCommand';
import { WithBigIntegerCommand } from '../TypeScript/Tests/TSTests/WithBigIntegerCommand';
import { AspNetResult } from '../CrisLike/AspNetResult';
import { AspNetCrisResultError } from '../CrisLike/AspNetCrisResultError';
import { UbiquitousValues } from '../CrisLike/UbiquitousValues';
import { SimpleUserMessage } from './SimpleUserMessage';
import { Guid } from '../../System/Guid';
import { ICommand } from '../Cris/Model';
import { ExtendedCultureInfo } from './ExtendedCultureInfo';
import { GrantLevel } from './GrantLevel';
import { TypeKind } from '../../Microsoft/CodeAnalysis/TypeKind';

export const SymCTS = Symbol.for("CK.CTSType");
/**
 * CTSType is currently &lt;any&gt;. Strongly typing it involves to handle null
 * (detect and raise error) in depth.
 * This is not a validator (the backend is up to date by design) and null handling
 * is a (basic) part of validation.
 */
export const CTSType : any = {
    get typeFilterName(): string {return "TypeScript"; },
    toTypedJson( o: any ) : [string,unknown]|null {
        if( o == null ) return null;
        const t = o[SymCTS];
        if( !t ) throw new Error( "Untyped object. A type must be specified with CTSType." );
        return [t.name, t.json( o )];
    },
    fromTypedJson( o: any ) : unknown {
        if( o == null ) return undefined;
        if( !(o instanceof Array && o.length === 2) ) throw new Error( "Expected 2-cells array." );
        const t = (<any>CTSType)[o[0]];
        if( !t ) throw new Error( `Invalid type name: ${o[0]}.` );
        if( !t.set ) throw new Error( `Type name '${o[0]}' is not serializable.` );
        const j = t.nosj( o[1] );
        return j !== null && typeof j === 'object' ? t.set( j ) : j;
   },
   stringify( o: any, withType: boolean = true ) : string {
       const t = CTSType.toTypedJson( o );
       return JSON.stringify( withType ? t : t[1] );
   },
   parse( s: string ) : unknown {
       return CTSType.fromTypedJson( JSON.parse( s ) );
   },
"CK.TypeScript.Tests.CrisLike.ISomeCommand": {
name: "CK.TypeScript.Tests.CrisLike.ISomeCommand",
set( o: SomeCommand ): SomeCommand {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {return o;
},
nosj( o: any ) {if( o == null ) return undefined;
return new SomeCommand(
CTSType["Guid"].nosj( o.actionId ),
CTSType["int"].nosj( o.actorId ) );
},
},
"CK.TypeScript.Tests.CrisLike.ISimpleCommand": {
name: "CK.TypeScript.Tests.CrisLike.ISimpleCommand",
set( o: SimpleCommand ): SimpleCommand {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {return o;
},
nosj( o: any ) {if( o == null ) return undefined;
return new SimpleCommand(
CTSType["string"].nosj( o.action ) );
},
},
"CK.TypeScript.Tests.CrisLike.ISimplestCommand": {
name: "CK.TypeScript.Tests.CrisLike.ISimplestCommand",
set( o: SimplestCommand ): SimplestCommand {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {return o;
},
nosj( o: any ) {if( o == null ) return undefined;
return new SimplestCommand(
 );
},
},
"CK.TypeScript.Tests.CrisLike.IWithObjectCommand": {
name: "CK.TypeScript.Tests.CrisLike.IWithObjectCommand",
set( o: WithObjectCommand ): WithObjectCommand {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {return o;
},
nosj( o: any ) {if( o == null ) return undefined;
return new WithObjectCommand(
CTSType["int"].nosj( o.power ),
CTSType["int"].nosj( o.powerPoco ),
CTSType["string"].nosj( o.deviceId ),
CTSType["int"].nosj( o.actorId ) );
},
},
"CK.TypeScript.Tests.CrisLike.IResult": {
name: "CK.TypeScript.Tests.CrisLike.IResult",
set( o: Result ): Result {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {return o;
},
nosj( o: any ) {if( o == null ) return undefined;
return new Result(
CTSType["int"].nosj( o.result ),
CTSType["string"].nosj( o.superResult ) );
},
},
"CK.TypeScript.Tests.CrisLike.IWithSecondaryCommand": {
name: "CK.TypeScript.Tests.CrisLike.IWithSecondaryCommand",
set( o: WithSecondaryCommand ): WithSecondaryCommand {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {return o;
},
nosj( o: any ) {if( o == null ) return undefined;
return new WithSecondaryCommand(
CTSType["CK.TypeScript.Tests.CrisLike.IWithObjectCommand"].nosj( o.nullableCmdWithSetter ),
CTSType["CK.TypeScript.Tests.CrisLike.IWithObjectCommand"].nosj( o.cmdWithSetter ),
CTSType["CK.TypeScript.Tests.CrisLike.IWithObjectCommand"].nosj( o.cmdAuto ),
CTSType["L(CK.TypeScript.Tests.CrisLike.IWithObjectCommand)"].nosj( o.listSecondary ),
CTSType["L(CK.TypeScript.Tests.CrisLike.IWithObjectCommand)"].nosj( o.listSecondaryAuto ) );
},
},
"CK.TypeScript.Tests.CrisLike.ICommandAbs": {
name: "CK.TypeScript.Tests.CrisLike.ICommandAbs",
},
"CK.TypeScript.Tests.CrisLike.IIntCommand": {
name: "CK.TypeScript.Tests.CrisLike.IIntCommand",
set( o: IntCommand ): IntCommand {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {if( !o ) return null;
let r = {} as any;
r.key = o.key;
r.keyList = o.keyList;
r.keySet = CTSType["S(int)"].json( o.keySet );
r.keyDictionary = CTSType["O(int)"].json( o.keyDictionary );
return r;
},
nosj( o: any ) {if( o == null ) return undefined;
return new IntCommand(
CTSType["int"].nosj( o.key ),
CTSType["L(int)"].nosj( o.keyList ),
CTSType["S(int)"].nosj( o.keySet ),
CTSType["O(int)"].nosj( o.keyDictionary ) );
},
},
"CK.TypeScript.Tests.CrisLike.IStringCommand": {
name: "CK.TypeScript.Tests.CrisLike.IStringCommand",
set( o: StringCommand ): StringCommand {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {if( !o ) return null;
let r = {} as any;
r.key = o.key;
r.keyList = o.keyList;
r.keySet = CTSType["S(string)"].json( o.keySet );
r.keyDictionary = CTSType["O(string)"].json( o.keyDictionary );
return r;
},
nosj( o: any ) {if( o == null ) return undefined;
return new StringCommand(
CTSType["string"].nosj( o.key ),
CTSType["L(string)"].nosj( o.keyList ),
CTSType["S(string)"].nosj( o.keySet ),
CTSType["O(string)"].nosj( o.keyDictionary ) );
},
},
"int": {
name: "int",
set( o: number ): number { o = Object( o ); (o as any)[SymCTS] = this; return o; },
json( o: any ) {return o; },
nosj( o: any ) {return o == null ? undefined : o; },
},
"string": {
name: "string",
set( o: string ): string { o = Object( o ); (o as any)[SymCTS] = this; return o; },
json( o: any ) {return o; },
nosj( o: any ) {return o == null ? undefined : o; },
},
"CK.TypeScript.Tests.CrisLike.NamedRecord": {
name: "CK.TypeScript.Tests.CrisLike.NamedRecord",
set( o: NamedRecord ): NamedRecord {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {return o;
},
nosj( o: any ) {if( o == null ) return undefined;
return new NamedRecord(
CTSType["int"].nosj( o.value ) ?? 0,
CTSType["string"].nosj( o.name ) ?? "" );
},
},
"CK.TypeScript.Tests.CrisLike.INamedRecordCommand": {
name: "CK.TypeScript.Tests.CrisLike.INamedRecordCommand",
set( o: NamedRecordCommand ): NamedRecordCommand {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {if( !o ) return null;
let r = {} as any;
r.key = o.key;
r.keyList = o.keyList;
r.keySet = CTSType["S(CK.TypeScript.Tests.CrisLike.NamedRecord)"].json( o.keySet );
r.keyDictionary = CTSType["O(CK.TypeScript.Tests.CrisLike.NamedRecord)"].json( o.keyDictionary );
return r;
},
nosj( o: any ) {if( o == null ) return undefined;
return new NamedRecordCommand(
CTSType["CK.TypeScript.Tests.CrisLike.NamedRecord"].nosj( o.key ),
CTSType["L(CK.TypeScript.Tests.CrisLike.NamedRecord)"].nosj( o.keyList ),
CTSType["S(CK.TypeScript.Tests.CrisLike.NamedRecord)"].nosj( o.keySet ),
CTSType["O(CK.TypeScript.Tests.CrisLike.NamedRecord)"].nosj( o.keyDictionary ) );
},
},
"CK.TypeScript.Tests.CrisLike.IAnonymousRecordCommand": {
name: "CK.TypeScript.Tests.CrisLike.IAnonymousRecordCommand",
set( o: AnonymousRecordCommand ): AnonymousRecordCommand {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {if( !o ) return null;
let r = {} as any;
r.key = CTSType["(int:Value,Guid:Id)"].json( o.key );
r.keyTuple = o.keyTuple;
r.keyHybrid = CTSType["(int,Guid:Id)"].json( o.keyHybrid );
r.keyList = CTSType["L((int:Value,Guid:Id))"].json( o.keyList );
r.keySet = CTSType["S((int:Value,Guid:Id))"].json( o.keySet );
r.keyDictionary = CTSType["O((int:Value,Guid:Id))"].json( o.keyDictionary );
return r;
},
nosj( o: any ) {if( o == null ) return undefined;
return new AnonymousRecordCommand(
CTSType["(int:Value,Guid:Id)"].nosj( o.key ),
CTSType["(int,Guid)"].nosj( o.keyTuple ),
CTSType["(int,Guid:Id)"].nosj( o.keyHybrid ),
CTSType["L((int:Value,Guid:Id))"].nosj( o.keyList ),
CTSType["S((int:Value,Guid:Id))"].nosj( o.keySet ),
CTSType["O((int:Value,Guid:Id))"].nosj( o.keyDictionary ) );
},
},
"CK.TypeScript.Tests.CrisLike.ICommandAbsWithNullableKey": {
name: "CK.TypeScript.Tests.CrisLike.ICommandAbsWithNullableKey",
},
"CK.TypeScript.Tests.CrisLike.ICommandCommand": {
name: "CK.TypeScript.Tests.CrisLike.ICommandCommand",
set( o: CommandCommand ): CommandCommand {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {if( !o ) return null;
let r = {} as any;
r.key = CTSType.toTypedJson( o.key );
r.keyList = CTSType["L(CK.CrisLike.ICommand)"].json( o.keyList );
r.keySet = CTSType["S(ExtendedCultureInfo)"].json( o.keySet );
r.keyDictionary = CTSType["O(CK.CrisLike.ICommand)"].json( o.keyDictionary );
return r;
},
nosj( o: any ) {if( o == null ) return undefined;
return new CommandCommand(
CTSType.fromTypedJson( o.key ),
CTSType["L(CK.CrisLike.ICommand)"].nosj( o.keyList ),
CTSType["S(ExtendedCultureInfo)"].nosj( o.keySet ),
CTSType["O(CK.CrisLike.ICommand)"].nosj( o.keyDictionary ) );
},
},
"CK.TypeScript.Tests.CrisLike.ICommandAbsWithResult": {
name: "CK.TypeScript.Tests.CrisLike.ICommandAbsWithResult",
},
"CK.TypeScript.Tests.CrisLike.INamedRecordWithResultCommand": {
name: "CK.TypeScript.Tests.CrisLike.INamedRecordWithResultCommand",
set( o: NamedRecordWithResultCommand ): NamedRecordWithResultCommand {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {if( !o ) return null;
let r = {} as any;
r.key = o.key;
r.keyList = o.keyList;
r.keySet = CTSType["S(CK.TypeScript.Tests.CrisLike.NamedRecord)"].json( o.keySet );
r.keyDictionary = CTSType["O(CK.TypeScript.Tests.CrisLike.NamedRecord)"].json( o.keyDictionary );
r.keyListOfNullable = o.keyListOfNullable;
return r;
},
nosj( o: any ) {if( o == null ) return undefined;
return new NamedRecordWithResultCommand(
CTSType["CK.TypeScript.Tests.CrisLike.NamedRecord"].nosj( o.key ),
CTSType["L(CK.TypeScript.Tests.CrisLike.NamedRecord)"].nosj( o.keyList ),
CTSType["S(CK.TypeScript.Tests.CrisLike.NamedRecord)"].nosj( o.keySet ),
CTSType["O(CK.TypeScript.Tests.CrisLike.NamedRecord)"].nosj( o.keyDictionary ),
CTSType["L(CK.TypeScript.Tests.CrisLike.NamedRecord?)"].nosj( o.keyListOfNullable ) );
},
},
"CK.TypeScript.Tests.TSTests.FullTSTests.ITestSerializationCommand": {
name: "CK.TypeScript.Tests.TSTests.FullTSTests.ITestSerializationCommand",
set( o: TestSerializationCommand ): TestSerializationCommand {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {if( !o ) return null;
let r = {} as any;
r.string = o.string;
r.int32 = o.int32;
r.single = o.single;
r.double = o.double;
r.long = CTSType["long"].json( o.long );
r.uLong = CTSType["ulong"].json( o.uLong );
r.bigInteger = CTSType["System.Numerics.BigInteger"].json( o.bigInteger );
r.grantLevel = o.grantLevel;
r.typeKind = o.typeKind;
r.guid = o.guid;
r.dateTime = o.dateTime;
r.timeSpan = CTSType["TimeSpan"].json( o.timeSpan );
r.simpleUserMessage = o.simpleUserMessage;
r.decimal = o.decimal;
return r;
},
nosj( o: any ) {if( o == null ) return undefined;
return new TestSerializationCommand(
CTSType["string"].nosj( o.string ),
CTSType["int"].nosj( o.int32 ),
CTSType["float"].nosj( o.single ),
CTSType["double"].nosj( o.double ),
CTSType["long"].nosj( o.long ),
CTSType["ulong"].nosj( o.uLong ),
CTSType["System.Numerics.BigInteger"].nosj( o.bigInteger ),
CTSType["CK.Core.GrantLevel"].nosj( o.grantLevel ),
CTSType["Microsoft.CodeAnalysis.TypeKind"].nosj( o.typeKind ),
CTSType["Guid"].nosj( o.guid ),
CTSType["DateTime"].nosj( o.dateTime ),
CTSType["TimeSpan"].nosj( o.timeSpan ),
CTSType["SimpleUserMessage"].nosj( o.simpleUserMessage ),
CTSType["decimal"].nosj( o.decimal ) );
},
},
"CK.TypeScript.Tests.TSTests.FullTSTests.IWithDecimalCommand": {
name: "CK.TypeScript.Tests.TSTests.FullTSTests.IWithDecimalCommand",
set( o: WithDecimalCommand ): WithDecimalCommand {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {return o;
},
nosj( o: any ) {if( o == null ) return undefined;
return new WithDecimalCommand(
CTSType["decimal"].nosj( o.value ) );
},
},
"CK.TypeScript.Tests.TSTests.FullTSTests.IWithULongCommand": {
name: "CK.TypeScript.Tests.TSTests.FullTSTests.IWithULongCommand",
set( o: WithULongCommand ): WithULongCommand {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {if( !o ) return null;
let r = {} as any;
r.value = CTSType["ulong"].json( o.value );
return r;
},
nosj( o: any ) {if( o == null ) return undefined;
return new WithULongCommand(
CTSType["ulong"].nosj( o.value ) );
},
},
"CK.TypeScript.Tests.TSTests.FullTSTests.IWithBigIntegerCommand": {
name: "CK.TypeScript.Tests.TSTests.FullTSTests.IWithBigIntegerCommand",
set( o: WithBigIntegerCommand ): WithBigIntegerCommand {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {if( !o ) return null;
let r = {} as any;
r.value = CTSType["System.Numerics.BigInteger"].json( o.value );
return r;
},
nosj( o: any ) {if( o == null ) return undefined;
return new WithBigIntegerCommand(
CTSType["System.Numerics.BigInteger"].nosj( o.value ) );
},
},
"AspNetResult": {
name: "AspNetResult",
set( o: AspNetResult ): AspNetResult {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {if( !o ) return null;
let r = {} as any;
r.result = CTSType.toTypedJson( o.result );
r.validationMessages = o.validationMessages;
r.correlationId = o.correlationId;
return r;
},
nosj( o: any ) {if( o == null ) return undefined;
return new AspNetResult(
CTSType.fromTypedJson( o.result ),
CTSType["L(SimpleUserMessage)"].nosj( o.validationMessages ),
CTSType["string"].nosj( o.correlationId ) );
},
},
"AspNetCrisResultError": {
name: "AspNetCrisResultError",
set( o: AspNetCrisResultError ): AspNetCrisResultError {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {return o;
},
nosj( o: any ) {if( o == null ) return undefined;
return new AspNetCrisResultError(
CTSType["bool"].nosj( o.isValidationError ),
CTSType["L(string)"].nosj( o.errors ),
CTSType["string"].nosj( o.logKey ) );
},
},
"CK.CrisLike.IUbiquitousValues": {
name: "CK.CrisLike.IUbiquitousValues",
set( o: UbiquitousValues ): UbiquitousValues {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {return o;
},
nosj( o: any ) {if( o == null ) return undefined;
return new UbiquitousValues(
 );
},
},
"CK.CrisLike.ICommand": {
name: "CK.CrisLike.ICommand",
},
"CK.CrisLike.ICommandAuthCritical": {
name: "CK.CrisLike.ICommandAuthCritical",
},
"CK.CrisLike.ICommandAuthNormal": {
name: "CK.CrisLike.ICommandAuthNormal",
},
"CK.CrisLike.ICommandAuthDeviceId": {
name: "CK.CrisLike.ICommandAuthDeviceId",
},
"CK.CrisLike.ICommandAuthUnsafe": {
name: "CK.CrisLike.ICommandAuthUnsafe",
},
"CK.CrisLike.ICommandPart": {
name: "CK.CrisLike.ICommandPart",
},
"CK.CrisLike.ICommand<CK.TypeScript.Tests.CrisLike.ISuperResult>": {
name: "CK.CrisLike.ICommand<CK.TypeScript.Tests.CrisLike.ISuperResult>",
},
"CK.CrisLike.ICommand<CK.TypeScript.Tests.CrisLike.IResult>": {
name: "CK.CrisLike.ICommand<CK.TypeScript.Tests.CrisLike.IResult>",
},
"object": {
name: "object",
},
"CK.CrisLike.ICommand<object>": {
name: "CK.CrisLike.ICommand<object>",
},
"CK.CrisLike.ICommand<int>": {
name: "CK.CrisLike.ICommand<int>",
},
"CK.CrisLike.IAbstractCommand": {
name: "CK.CrisLike.IAbstractCommand",
},
"CK.CrisLike.ICrisPoco": {
name: "CK.CrisLike.ICrisPoco",
},
"CK.Core.IPoco": {
name: "CK.Core.IPoco",
},
"SimpleUserMessage": {
name: "SimpleUserMessage",
set( o: SimpleUserMessage ): SimpleUserMessage {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {return o; },
nosj( o: any ) {return o != null ? SimpleUserMessage.parse( o ) : undefined; },
},
"L(SimpleUserMessage)": {
name: "L(SimpleUserMessage)",
set( o: Array<SimpleUserMessage> ): Array<SimpleUserMessage> {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {
return o;
},
nosj( o: any ) {
if( o == null ) return undefined;
if( !(o instanceof Array) ) throw new Error( 'Expected Array.' );
const t = CTSType["SimpleUserMessage"];
return o.map( t.nosj );
},
},
"bool": {
name: "bool",
set( o: boolean ): boolean { o = Object( o ); (o as any)[SymCTS] = this; return o; },
json( o: any ) {return o; },
nosj( o: any ) {return o == null ? undefined : o; },
},
"L(string?)": {
name: "L(string?)",
set( o: Array<string|undefined> ): Array<string|undefined> {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {
return o;
},
nosj( o: any ) {
if( o == null ) return undefined;
if( !(o instanceof Array) ) throw new Error( 'Expected Array.' );
const t = CTSType["string"];
return o.map( t.nosj );
},
},
"L(string)": {
name: "L(string)",
json( o: any ) {
return o;
},
nosj( o: any ) {
if( o == null ) return undefined;
if( !(o instanceof Array) ) throw new Error( 'Expected Array.' );
const t = CTSType["string"];
return o.map( t.nosj );
},
},
"Guid": {
name: "Guid",
set( o: Guid ): Guid {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {return o; },
nosj( o: any ) {return o != null ? new Guid( o ) : undefined; },
},
"(int,Guid)": {
name: "(int,Guid)",
set( o: [number, Guid] ): [number, Guid] {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {return o;
},
nosj( o: any ) {if( o == null ) return undefined;
return [
CTSType["int"].nosj( o[0] ) ?? 0,
CTSType["Guid"].nosj( o[1] ) ?? Guid.empty];
},
},
"(int:Value,Guid:Id)": {
name: "(int:Value,Guid:Id)",
json( o: any ) {if( !o ) return null;
return [ o.value, o.id ];
},
nosj( o: any ) {if( o == null ) return undefined;
return {
value: CTSType["int"].nosj( o[0] ) ?? 0,
id: CTSType["Guid"].nosj( o[1] ) ?? Guid.empty};
},
},
"(int,Guid:Id)": {
name: "(int,Guid:Id)",
json( o: any ) {if( !o ) return null;
return [ o.item1, o.id ];
},
nosj( o: any ) {if( o == null ) return undefined;
return {
item1: CTSType["int"].nosj( o[0] ) ?? 0,
id: CTSType["Guid"].nosj( o[1] ) ?? Guid.empty};
},
},
"L((int,Guid))": {
name: "L((int,Guid))",
set( o: Array<[number, Guid]> ): Array<[number, Guid]> {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {
return o;
},
nosj( o: any ) {
if( o == null ) return undefined;
if( !(o instanceof Array) ) throw new Error( 'Expected Array.' );
const t = CTSType["(int,Guid)"];
return o.map( t.nosj );
},
},
"L((int:Value,Guid:Id))": {
name: "L((int:Value,Guid:Id))",
json( o: any ) {
if( o == null ) return null;
const t = CTSType["(int:Value,Guid:Id)"];
return o.map( t.json );
},
nosj( o: any ) {
if( o == null ) return undefined;
if( !(o instanceof Array) ) throw new Error( 'Expected Array.' );
const t = CTSType["(int:Value,Guid:Id)"];
return o.map( t.nosj );
},
},
"S((int,Guid))": {
name: "S((int,Guid))",
set( o: Set<[number, Guid]> ): Set<[number, Guid]> {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {
if( !o ) return null;
const t = CTSType["(int,Guid)"];
return Array.from( o ).map( t.json );
},
nosj( o: any ) {
if( o == null ) return undefined;
if( !(o instanceof Array) ) throw new Error( 'Expected Array.' );
const t = CTSType["(int,Guid)"];
return new Set( o.map( t.nosj ) );
},
},
"S((int:Value,Guid:Id))": {
name: "S((int:Value,Guid:Id))",
json( o: any ) {
if( !o ) return null;
const t = CTSType["(int:Value,Guid:Id)"];
return Array.from( o ).map( t.json );
},
nosj( o: any ) {
if( o == null ) return undefined;
if( !(o instanceof Array) ) throw new Error( 'Expected Array.' );
const t = CTSType["(int:Value,Guid:Id)"];
return new Set( o.map( t.nosj ) );
},
},
"O((int,Guid))": {
name: "O((int,Guid))",
set( o: Map<string,[number, Guid]> ): Map<string,[number, Guid]> {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {
return o != null ? Object.fromEntries(o.entries()) : null;
},
nosj( o: any ) {
if( o == null ) return undefined;
const isA = o instanceof Array;
if( !isA && typeof o !== 'object' ) throw new Error( 'Expected Array or Object.' );
if( isA ) {
const t = CTSType["(int,Guid)"];
const r = new Map();
for( const i of o ) {
    r.set( i[0], t.nosj(i[1]) );
}
return r;
}
const t = CTSType["(int,Guid)"];
const r = new Map();
for( const p in o ) {
    r.set( p, t.nosj(o[p]) );
}
return r;
},
},
"O((int:Value,Guid:Id))": {
name: "O((int:Value,Guid:Id))",
json( o: any ) {
if( !o ) return null;
const t = CTSType["(int:Value,Guid:Id)"];
let r = {} as any;
for( const i of o ) {
    r[i[0]] = t.json(i[1]);
}
return r;
},
nosj( o: any ) {
if( o == null ) return undefined;
const isA = o instanceof Array;
if( !isA && typeof o !== 'object' ) throw new Error( 'Expected Array or Object.' );
if( isA ) {
const t = CTSType["(int:Value,Guid:Id)"];
const r = new Map();
for( const i of o ) {
    r.set( i[0], t.nosj(i[1]) );
}
return r;
}
const t = CTSType["(int:Value,Guid:Id)"];
const r = new Map();
for( const p in o ) {
    r.set( p, t.nosj(o[p]) );
}
return r;
},
},
"L(CK.CrisLike.ICommand?)": {
name: "L(CK.CrisLike.ICommand?)",
set( o: Array<ICommand|undefined> ): Array<ICommand|undefined> {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {
return o != null ? o.map( CTSType.toTypedJson ) : null;
},
nosj( o: any ) {
if( o == null ) return undefined;
if( !(o instanceof Array) ) throw new Error( 'Expected Array.' );
return o.map( CTSType.fromTypedJson );
},
},
"L(CK.CrisLike.ICommand)": {
name: "L(CK.CrisLike.ICommand)",
json( o: any ) {
return o != null ? o.map( CTSType.toTypedJson ) : null;
},
nosj( o: any ) {
if( o == null ) return undefined;
if( !(o instanceof Array) ) throw new Error( 'Expected Array.' );
return o.map( CTSType.fromTypedJson );
},
},
"ExtendedCultureInfo": {
name: "ExtendedCultureInfo",
set( o: ExtendedCultureInfo ): ExtendedCultureInfo {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {return o; },
nosj( o: any ) {return o != null ? new ExtendedCultureInfo( o ) : undefined; },
},
"S(ExtendedCultureInfo?)": {
name: "S(ExtendedCultureInfo?)",
set( o: Set<ExtendedCultureInfo|undefined> ): Set<ExtendedCultureInfo|undefined> {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {
return o != null ? Array.from( o ).map( CTSType.toTypedJson ) : null;
},
nosj( o: any ) {
if( o == null ) return undefined;
if( !(o instanceof Array) ) throw new Error( 'Expected Array.' );
return new Set( o.map( CTSType.fromTypedJson ) );
},
},
"S(ExtendedCultureInfo)": {
name: "S(ExtendedCultureInfo)",
json( o: any ) {
return o != null ? Array.from( o ).map( CTSType.toTypedJson ) : null;
},
nosj( o: any ) {
if( o == null ) return undefined;
if( !(o instanceof Array) ) throw new Error( 'Expected Array.' );
return new Set( o.map( CTSType.fromTypedJson ) );
},
},
"O(CK.CrisLike.ICommand?)": {
name: "O(CK.CrisLike.ICommand?)",
set( o: Map<string,ICommand|undefined> ): Map<string,ICommand|undefined> {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {
if( !o ) return null;
let r = {} as any;
for( const i of o ) {
    r[i[0]] = CTSType.toTypedJson(i[1]);
}
return r;
},
nosj( o: any ) {
if( o == null ) return undefined;
const isA = o instanceof Array;
if( !isA && typeof o !== 'object' ) throw new Error( 'Expected Array or Object.' );
if( isA ) {
const r = new Map();
for( const i of o ) {
    r.set( i[0], CTSType.fromTypedJson(i[1]) );
}
return r;
}
const r = new Map();
for( const p in o ) {
    r.set( p, CTSType.fromTypedJson(o[p]) );
}
return r;
},
},
"O(CK.CrisLike.ICommand)": {
name: "O(CK.CrisLike.ICommand)",
json( o: any ) {
if( !o ) return null;
let r = {} as any;
for( const i of o ) {
    r[i[0]] = CTSType.toTypedJson(i[1]);
}
return r;
},
nosj( o: any ) {
if( o == null ) return undefined;
const isA = o instanceof Array;
if( !isA && typeof o !== 'object' ) throw new Error( 'Expected Array or Object.' );
if( isA ) {
const r = new Map();
for( const i of o ) {
    r.set( i[0], CTSType.fromTypedJson(i[1]) );
}
return r;
}
const r = new Map();
for( const p in o ) {
    r.set( p, CTSType.fromTypedJson(o[p]) );
}
return r;
},
},
"L(int)": {
name: "L(int)",
set( o: Array<number> ): Array<number> {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {
return o;
},
nosj( o: any ) {
if( o == null ) return undefined;
if( !(o instanceof Array) ) throw new Error( 'Expected Array.' );
const t = CTSType["int"];
return o.map( t.nosj );
},
},
"S(int)": {
name: "S(int)",
set( o: Set<number> ): Set<number> {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {
if( !o ) return null;
const t = CTSType["int"];
return Array.from( o ).map( t.json );
},
nosj( o: any ) {
if( o == null ) return undefined;
if( !(o instanceof Array) ) throw new Error( 'Expected Array.' );
const t = CTSType["int"];
return new Set( o.map( t.nosj ) );
},
},
"O(int)": {
name: "O(int)",
set( o: Map<string,number> ): Map<string,number> {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {
return o != null ? Object.fromEntries(o.entries()) : null;
},
nosj( o: any ) {
if( o == null ) return undefined;
const isA = o instanceof Array;
if( !isA && typeof o !== 'object' ) throw new Error( 'Expected Array or Object.' );
if( isA ) {
const t = CTSType["int"];
const r = new Map();
for( const i of o ) {
    r.set( i[0], t.nosj(i[1]) );
}
return r;
}
const t = CTSType["int"];
const r = new Map();
for( const p in o ) {
    r.set( p, t.nosj(o[p]) );
}
return r;
},
},
"L(CK.TypeScript.Tests.CrisLike.NamedRecord)": {
name: "L(CK.TypeScript.Tests.CrisLike.NamedRecord)",
set( o: Array<NamedRecord> ): Array<NamedRecord> {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {
return o;
},
nosj( o: any ) {
if( o == null ) return undefined;
if( !(o instanceof Array) ) throw new Error( 'Expected Array.' );
const t = CTSType["CK.TypeScript.Tests.CrisLike.NamedRecord"];
return o.map( t.nosj );
},
},
"S(CK.TypeScript.Tests.CrisLike.NamedRecord)": {
name: "S(CK.TypeScript.Tests.CrisLike.NamedRecord)",
set( o: Set<NamedRecord> ): Set<NamedRecord> {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {
if( !o ) return null;
const t = CTSType["CK.TypeScript.Tests.CrisLike.NamedRecord"];
return Array.from( o ).map( t.json );
},
nosj( o: any ) {
if( o == null ) return undefined;
if( !(o instanceof Array) ) throw new Error( 'Expected Array.' );
const t = CTSType["CK.TypeScript.Tests.CrisLike.NamedRecord"];
return new Set( o.map( t.nosj ) );
},
},
"O(CK.TypeScript.Tests.CrisLike.NamedRecord)": {
name: "O(CK.TypeScript.Tests.CrisLike.NamedRecord)",
set( o: Map<string,NamedRecord> ): Map<string,NamedRecord> {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {
return o != null ? Object.fromEntries(o.entries()) : null;
},
nosj( o: any ) {
if( o == null ) return undefined;
const isA = o instanceof Array;
if( !isA && typeof o !== 'object' ) throw new Error( 'Expected Array or Object.' );
if( isA ) {
const t = CTSType["CK.TypeScript.Tests.CrisLike.NamedRecord"];
const r = new Map();
for( const i of o ) {
    r.set( i[0], t.nosj(i[1]) );
}
return r;
}
const t = CTSType["CK.TypeScript.Tests.CrisLike.NamedRecord"];
const r = new Map();
for( const p in o ) {
    r.set( p, t.nosj(o[p]) );
}
return r;
},
},
"L(CK.TypeScript.Tests.CrisLike.NamedRecord?)": {
name: "L(CK.TypeScript.Tests.CrisLike.NamedRecord?)",
set( o: Array<NamedRecord|undefined> ): Array<NamedRecord|undefined> {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {
return o;
},
nosj( o: any ) {
if( o == null ) return undefined;
if( !(o instanceof Array) ) throw new Error( 'Expected Array.' );
const t = CTSType["CK.TypeScript.Tests.CrisLike.NamedRecord"];
return o.map( t.nosj );
},
},
"S(string?)": {
name: "S(string?)",
set( o: Set<string|undefined> ): Set<string|undefined> {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {
if( !o ) return null;
const t = CTSType["string"];
return Array.from( o ).map( t.json );
},
nosj( o: any ) {
if( o == null ) return undefined;
if( !(o instanceof Array) ) throw new Error( 'Expected Array.' );
const t = CTSType["string"];
return new Set( o.map( t.nosj ) );
},
},
"S(string)": {
name: "S(string)",
json( o: any ) {
if( !o ) return null;
const t = CTSType["string"];
return Array.from( o ).map( t.json );
},
nosj( o: any ) {
if( o == null ) return undefined;
if( !(o instanceof Array) ) throw new Error( 'Expected Array.' );
const t = CTSType["string"];
return new Set( o.map( t.nosj ) );
},
},
"O(string?)": {
name: "O(string?)",
set( o: Map<string,string|undefined> ): Map<string,string|undefined> {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {
return o != null ? Object.fromEntries(o.entries()) : null;
},
nosj( o: any ) {
if( o == null ) return undefined;
const isA = o instanceof Array;
if( !isA && typeof o !== 'object' ) throw new Error( 'Expected Array or Object.' );
if( isA ) {
const t = CTSType["string"];
const r = new Map();
for( const i of o ) {
    r.set( i[0], t.nosj(i[1]) );
}
return r;
}
const t = CTSType["string"];
const r = new Map();
for( const p in o ) {
    r.set( p, t.nosj(o[p]) );
}
return r;
},
},
"O(string)": {
name: "O(string)",
json( o: any ) {
return o != null ? Object.fromEntries(o.entries()) : null;
},
nosj( o: any ) {
if( o == null ) return undefined;
const isA = o instanceof Array;
if( !isA && typeof o !== 'object' ) throw new Error( 'Expected Array or Object.' );
if( isA ) {
const t = CTSType["string"];
const r = new Map();
for( const i of o ) {
    r.set( i[0], t.nosj(i[1]) );
}
return r;
}
const t = CTSType["string"];
const r = new Map();
for( const p in o ) {
    r.set( p, t.nosj(o[p]) );
}
return r;
},
},
"L(CK.TypeScript.Tests.CrisLike.IWithObjectCommand?)": {
name: "L(CK.TypeScript.Tests.CrisLike.IWithObjectCommand?)",
set( o: Array<WithObjectCommand|undefined> ): Array<WithObjectCommand|undefined> {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {
return o;
},
nosj( o: any ) {
if( o == null ) return undefined;
if( !(o instanceof Array) ) throw new Error( 'Expected Array.' );
const t = CTSType["CK.TypeScript.Tests.CrisLike.IWithObjectCommand"];
return o.map( t.nosj );
},
},
"L(CK.TypeScript.Tests.CrisLike.IWithObjectCommand)": {
name: "L(CK.TypeScript.Tests.CrisLike.IWithObjectCommand)",
json( o: any ) {
return o;
},
nosj( o: any ) {
if( o == null ) return undefined;
if( !(o instanceof Array) ) throw new Error( 'Expected Array.' );
const t = CTSType["CK.TypeScript.Tests.CrisLike.IWithObjectCommand"];
return o.map( t.nosj );
},
},
"float": {
name: "float",
set( o: number ): number { o = Object( o ); (o as any)[SymCTS] = this; return o; },
json( o: any ) {return o; },
nosj( o: any ) {return o == null ? undefined : o; },
},
"double": {
name: "double",
set( o: number ): number { o = Object( o ); (o as any)[SymCTS] = this; return o; },
json( o: any ) {return o; },
nosj( o: any ) {return o == null ? undefined : o; },
},
"long": {
name: "long",
set( o: bigint ): bigint { o = Object( o ); (o as any)[SymCTS] = this; return o; },
json( o: any ) {return o != null ? o.toString() : null; },
nosj( o: any ) {return o != null ? BigInt( o ) : undefined; },
},
"ulong": {
name: "ulong",
set( o: bigint ): bigint { o = Object( o ); (o as any)[SymCTS] = this; return o; },
json( o: any ) {return o != null ? o.toString() : null; },
nosj( o: any ) {return o != null ? BigInt( o ) : undefined; },
},
"System.Numerics.BigInteger": {
name: "System.Numerics.BigInteger",
set( o: bigint ): bigint { o = Object( o ); (o as any)[SymCTS] = this; return o; },
json( o: any ) {return o != null ? o.toString() : null; },
nosj( o: any ) {return o != null ? BigInt( o ) : undefined; },
},
"byte": {
name: "byte",
set( o: number ): number { o = Object( o ); (o as any)[SymCTS] = this; return o; },
json( o: any ) {return o; },
nosj( o: any ) {return o == null ? undefined : o; },
},
"CK.Core.GrantLevel": {
name: "CK.Core.GrantLevel",
set( o: GrantLevel ): GrantLevel { o = <GrantLevel>new Number( o ); (o as any)[SymCTS] = this; return o; },
json( o: any ) { return o; },
nosj( o: any ) { return o == null ? undefined : o; },
},
"Microsoft.CodeAnalysis.TypeKind": {
name: "Microsoft.CodeAnalysis.TypeKind",
set( o: TypeKind ): TypeKind { o = <TypeKind>new Number( o ); (o as any)[SymCTS] = this; return o; },
json( o: any ) { return o; },
nosj( o: any ) { return o == null ? undefined : o; },
},
"DateTime": {
name: "DateTime",
set( o: DateTime ): DateTime {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {return o; },
nosj( o: any ) {return o != null ? DateTime.fromISO( o, {zone: 'UTC'}) : undefined; },
},
"TimeSpan": {
name: "TimeSpan",
set( o: Duration ): Duration {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {return o != null ? o.toMillis().toString()+'0000' : null; },
nosj( o: any ) {return o != null ? Duration.fromMillis( Number.parseInt(o.substring(0,o.length-4)) ) : undefined; },
},
"decimal": {
name: "decimal",
set( o: Decimal ): Decimal {  (o as any)[SymCTS] = this; return o; },
json( o: any ) {return o; },
nosj( o: any ) {return o != null ? new Decimal( o ) : undefined; },
},
}
// This configures the default Decimal to be compliant with .Net Decimal
// type. Precision is boosted from 20 to 29 and toExpNeg/Pos are set so 
// that values out of range with .Net type will be toString() and toJSON() 
// with an exponential notation (that will fail the .Net parsing).
// However nothing prevents a javascript Decimal to be out of range but
// in this case, parsing on the .Net side will fail. For instance:
// Decimal.MaxValue + 1 = 79228162514264337593543950336 will fail regardless
// of the notation used.
Decimal.set({
      precision: Math.max( 29, Decimal.precision ), 
      toExpNeg: Math.min( -29, Decimal.toExpNeg ),
      toExpPos: Math.max( 29, Decimal.toExpPos )
}); 
