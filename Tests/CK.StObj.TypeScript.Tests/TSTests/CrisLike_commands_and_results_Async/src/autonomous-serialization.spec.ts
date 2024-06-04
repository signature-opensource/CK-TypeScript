import { CTSType, Guid, UserMessageLevel, NamedRecord, NamedRecordCommand, AnonymousRecordCommand, CommandCommand, SomeCommand, SimplestCommand, SimpleCommand, ExtendedCultureInfo, GrantLevel, TypeKind } from "@local/ck-gen";
import { DateTime, Duration } from "luxon";
import { Decimal } from "decimal.js-light";
import { TestSerializationCommand } from "@local/ck-gen";
import { SimpleUserMessage } from "@local/ck-gen";

// Trick from https://stackoverflow.com/a/77047461/190380
// When debugging ("Debug Test at Cursor" in menu), this cancels jest timeout.
if( process.env.VSCODE_INSPECTOR_OPTIONS ) {
  jest.setTimeout(30 * 60 * 1000 ); // 30 minutes
}

it('Set must use Array.from', () => {
  const set = new Set<string>();
  set.add( "one" );
  set.add( "two" );

  const sNoWay = JSON.stringify( set );
  expect(sNoWay).toBe( '{}' );

  const s = JSON.stringify( Array.from(set) );
  expect(s).toBe( '["one","two"]' );
});
it('Map can use Array.from when no projection must be done.', () => {
  const map = new Map<string,number>();
  map.set( "one", 1 );
  map.set( "two", 2 );

  const sNoWay = JSON.stringify( map );
  expect(sNoWay).toBe( '{}' );

  const s = JSON.stringify( Array.from(map) );
  expect(s).toBe( '[["one",1],["two",2]]' );
});
it('Map builds the array when projection must be done.', () => {
  const map = new Map<string,number>();
  map.set( "one", 1 );
  map.set( "two", 2 );

  function f(o:any) { return o + o; }

  const a = new Array<[any,any]>;
  for (const i of map) {
    a.push([f(i[0]),f(i[1])]);
  }

  const s = JSON.stringify( a );
  expect(s).toBe( '[["oneone",2],["twotwo",4]]' );
});
it('O map can use Object.fromEntries when no projection must be done.', () => {
  const map = new Map<string,number>();
  map.set( "one", 1 );
  map.set( "two", 2 );
  const s = JSON.stringify( Object.fromEntries(map.entries()) );
  expect(s).toBe( '{"one":1,"two":2}' );
});
it('O map with projection can use 2 methods.', () => {
  const map = new Map<string,number>();
  map.set( "one", 1 );
  map.set( "two", 2 );
  
  function f(o:any) { return o*2; }
  
  const a = new Array<[string,any]>;
  for (const i of map) {
    a.push([i[0],f(i[1])]);
  }
  const s = JSON.stringify( Object.fromEntries(a) );
  expect(s).toBe( '{"one":2,"two":4}' );

  let o = {};
  for (const i of map) {
    o[i[0]] = f(i[1]);
  }
  const s2 = JSON.stringify( o );
  expect(s2).toBe( '{"one":2,"two":4}' );
});


// Sample test.
describe('Command serialization', () => {

  it('Command with basic types.', () => {
      var c = new TestSerializationCommand( 
        "A string", 
        42,
        3.7, 
        Math.PI, 
        9999n, 
        -87878n,
        99999999n, 
        GrantLevel.Contributor,
        TypeKind.FunctionPointer,
        new Guid("d0acf1b1-4675-4a23-af51-3c834d910f3d"),
        DateTime.utc(2024,3,6,13,26,12,854),
        Duration.fromMillis(3712),
        new SimpleUserMessage(UserMessageLevel.Info,"Hello!"),
        new Decimal( "79228162514264337593543950335" ) );

      const json = CTSType.toTypedJson( c );
      const s = JSON.stringify(json);
      expect(s).toEqual( '["CK.StObj.TypeScript.Tests.TSTests.FullTSTests.ITestSerializationCommand",{"string":"A string","int32":42,"single":3.7,"double":3.141592653589793,"long":"9999","uLong":"-87878","bigInteger":"99999999","grantLevel":32,"typeKind":13,"guid":"d0acf1b1-4675-4a23-af51-3c834d910f3d","dateTime":"2024-03-06T13:26:12.854Z","timeSpan":"37120000","simpleUserMessage":[4,"Hello!",0],"decimal":"79228162514264337593543950335"}]' );
      const raw = JSON.parse(s);
      const back = CTSType.fromTypedJson( raw );
      expect( back ).toStrictEqual( c );
    });

    it( 'Command with records.', () => {
      const c = new NamedRecordCommand();
      c.key.name = "Hello";
      c.key.value = 3712;
      c.keyList.push( c.key, new NamedRecord( 42, 'another' ) );
      c.keySet.add( new NamedRecord(1, 'One') );
      c.keySet.add( new NamedRecord(2, 'Two') );
      c.keyDictionary.set( 'n°1', c.key );
      c.keyDictionary.set( 'n°2', new NamedRecord(-3712,'Last one'));
      const json = CTSType.stringify( c );
      expect(json).toEqual('["CK.StObj.TypeScript.Tests.CrisLike.INamedRecordCommand",{"key":{"value":3712,"name":"Hello"},"keyList":[{"value":3712,"name":"Hello"},{"value":42,"name":"another"}],"keySet":[{"value":1,"name":"One"},{"value":2,"name":"Two"}],"keyDictionary":{"n°1":{"value":3712,"name":"Hello"},"n°2":{"value":-3712,"name":"Last one"}}}]');
      const back = CTSType.parse( json );      
      expect( back ).toStrictEqual( c );
    });

    it( 'Command with anonymous records.', () => {
      const c = new AnonymousRecordCommand();
      c.key.value = 3712;
      c.key.id = Guid.empty;
      c.keyTuple[0] = 42;
      c.keyTuple[1] = new Guid( "some guid..." );
      c.keyHybrid.item1 = 42;
      c.keyHybrid.id = new Guid( "some other guid." );
      c.keyList.push( c.key, {value:42, id: new Guid("our simple guid allow any string")} );
      c.keySet.add( {value: 1, id: new Guid("in set 1")} );
      c.keySet.add({value: 27, id: new Guid("in set 2")}  );
      c.keyDictionary.set( 'n°1', {value: -1, id: new Guid("in dic 1")} );
      c.keyDictionary.set( 'n°2', {value: -2, id: new Guid("in dic 1")});
      const json = CTSType.stringify( c );
      expect(json).toEqual('["CK.StObj.TypeScript.Tests.CrisLike.IAnonymousRecordCommand",{"key":[3712,"00000000-0000-0000-0000-000000000000"],"keyTuple":[42,"some guid..."],"keyHybrid":[42,"some other guid."],"keyList":[[3712,"00000000-0000-0000-0000-000000000000"],[42,"our simple guid allow any string"]],"keySet":[[1,"in set 1"],[27,"in set 2"]],"keyDictionary":{"n°1":[-1,"in dic 1"],"n°2":[-2,"in dic 1"]}}]');
      const back = CTSType.parse( json );      
      expect( back ).toStrictEqual( c );
    });

    it( 'Command with commands.', () => {
      const c = new CommandCommand();
      expect( c.key ).toBeUndefined();
      const json1 = CTSType.stringify( c );
      expect(json1).toEqual('["CK.StObj.TypeScript.Tests.CrisLike.ICommandCommand",{"key":null,"keyList":[],"keySet":[],"keyDictionary":{}}]');
      const back1 = CTSType.parse( json1 );
      expect( back1 ).toStrictEqual( c );

      c.key = new SomeCommand(new Guid("...guid..."), 3712 );
      const json2 = CTSType.stringify( c );
      expect(json2).toEqual('["CK.StObj.TypeScript.Tests.CrisLike.ICommandCommand",{"key":["CK.StObj.TypeScript.Tests.CrisLike.ISomeCommand",{"actionId":"...guid...","actorId":3712}],"keyList":[],"keySet":[],"keyDictionary":{}}]');
      const back2 = CTSType.parse( json2 );
      expect( back2 ).toStrictEqual( c );

      c.keyList.push( c.key, new SimplestCommand(), new SimpleCommand("do!") );
      const json3 = CTSType.stringify( c );
      expect(json3).toEqual('["CK.StObj.TypeScript.Tests.CrisLike.ICommandCommand",{"key":["CK.StObj.TypeScript.Tests.CrisLike.ISomeCommand",{"actionId":"...guid...","actorId":3712}],"keyList":[["CK.StObj.TypeScript.Tests.CrisLike.ISomeCommand",{"actionId":"...guid...","actorId":3712}],["CK.StObj.TypeScript.Tests.CrisLike.ISimplestCommand",{}],["CK.StObj.TypeScript.Tests.CrisLike.ISimpleCommand",{"action":"do!"}]],"keySet":[],"keyDictionary":{}}]');
      const back3 = CTSType.parse( json3 );
      expect( back3 ).toStrictEqual( c );

      c.keyDictionary.set("n°1", c.keyList[0]);
      c.keyDictionary.set("n°2", c.keyList[1]);
      c.keyDictionary.set("n°3", c.keyList[2]);
      c.keySet.add( new ExtendedCultureInfo("fr") );
      c.keySet.add( new ExtendedCultureInfo("de") );
      c.keySet.add( new ExtendedCultureInfo("es") );
      c.keySet.add( new ExtendedCultureInfo("en") );

      const json4 = CTSType.stringify( c );
      const back4 = CTSType.parse( json4 );
      expect( back4 ).toStrictEqual( c );

    });

  });