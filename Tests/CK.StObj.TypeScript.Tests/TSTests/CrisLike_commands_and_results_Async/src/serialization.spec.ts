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
it('Map must use Array.from', () => {
  const map = new Map<string,number>();
  map.set( "one", 1 );
  map.set( "two", 2 );

  const sNoWay = JSON.stringify( map );
  expect(sNoWay).toBe( '{}' );

  const s = JSON.stringify( Array.from(map) );
  expect(s).toBe( '[["one",1],["two",2]]' );
});
it('O map must use Object.fromEntries.', () => {
  const map = new Map<string,number>();
  map.set( "one", 1 );
  map.set( "two", 2 );
  const s = JSON.stringify( Object.fromEntries(map.entries()) );
  expect(s).toBe( '{"one":1,"two":2}' );
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
        DateTime.fromISO('2024-01-19T18:43.7',{zone:'utc'}),
        Duration.fromMillis(3712) );

      const s = CTSType.typedJson( c );
      
      expect(s).toBe( '["CK.StObj.TypeScript.Tests.TSTests.FullTSTests.ITestSerializationCommand",{"string":"A string","int32":42,"single":3.7,"double":3.141592653589793,"guid":"d0acf1b1-4675-4a23-af51-3c834d910f3d","dateTime":null,"timeSpan":"PT3.712S"}]' );
    });
  });