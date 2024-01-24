import { ICommandAbs, IntCommand, StringCommand } from "@local/ck-gen";

// Sample test.
describe('Command serialization', () => {
    it('should be true', () => {
      const cI = new IntCommand( 3712 );
      var cA: ICommandAbs = cI; 
      expect(typeof cA.key).toBe( "number" );
      expect(cA.key).toBe( 3712 );
 
      const cS = new StringCommand( "Hello!" );
      cA = cS;
      expect(typeof cA.key).toBe( "string" );
      expect(cA.key).toBe( "Hello!" );
    });
  });