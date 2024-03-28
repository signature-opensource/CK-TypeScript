import { CTSType, Guid, IPoco, UserMessageLevel, SymCTS } from "@local/ck-gen";
import { DateTime, Duration } from "luxon";
import { TestSerializationCommand } from "@local/ck-gen";
import { SimpleUserMessage } from "@local/ck-gen";

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
    it('should be true', () => {
      var c = new TestSerializationCommand( 
        "A string", 
        42,
        3.7, 
        Math.PI, 
        new Guid("d0acf1b1-4675-4a23-af51-3c834d910f3d"),
        DateTime.utc(2024,3,6,13,26,12,854),
        Duration.fromMillis(3712),
        new SimpleUserMessage(UserMessageLevel.Info,"Hello!") );

      const json = CTSType.typedJson( c );
      const s = JSON.stringify(json);
      expect(s).toEqual( '["CK.StObj.TypeScript.Tests.TSTests.FullTSTests.ITestSerializationCommand",{"string":"A string","int32":42,"single":3.7,"double":3.141592653589793,"guid":"d0acf1b1-4675-4a23-af51-3c834d910f3d","dateTime":"2024-03-06T13:26:12.854Z","timeSpan":"37120000","simpleUserMessage":[4,"Hello!",0]}]' );
    });
  });