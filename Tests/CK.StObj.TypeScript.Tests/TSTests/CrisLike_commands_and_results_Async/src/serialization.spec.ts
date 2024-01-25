import { Guid, IPoco, IPocoType } from "@local/ck-gen";
import { DateTime, Duration } from "luxon";
import { TestSerializationCommand } from "@local/ck-gen";


export interface IPocoJSONSerializer {
  getReplacer(): (key:string,value:any) => any;
}



class DefaultJsonSerializer implements IPocoJSONSerializer
{
  getReplacer(): (key: string, value: any) => any {

    return function(key: string, value: any) {
      console.debug(this);
      return value;
    };
  }

  
  getTyped( o: unknown ) : unknown {
    if( o === null || typeof o === "undefined" ) return null;
    if( typeof o === "number" || typeof o === "string" || typeof(o) === "boolean" || typeof o === "bigint" ) return o;
    if( typeof o === "function" || typeof o === "symbol" ) throw new Error( "Function or Symbol are not supported." );
    if( o instanceof Array )
    {

    }
    
  }


} 

export class PocoSerializer {
  static getDefaultJsonSerializer(): IPocoJSONSerializer {
    return PocoSerializer._default;
  }

  private static _default = new DefaultJsonSerializer(); 
}

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

      const s = JSON.stringify(c,PocoSerializer.getDefaultJsonSerializer().getReplacer() );
      expect(s).toBe( '{"string":"A string","int32":42,"single":3.7,"double":3.141592653589793,"guid":"d0acf1b1-4675-4a23-af51-3c834d910f3d","dateTime":null,"timeSpan":"PT3.712S"}' );
    });
  });