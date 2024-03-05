import { ICommandAbs, IntCommand, StringCommand, NamedRecordCommand, NamedRecord, SymCTS } from "@local/ck-gen";
 

// Sample test.
describe('Abstract commands', () => {
    it('can have abstract read only properties', () => {
      let cI = new IntCommand( 3712 );
      let cA: ICommandAbs = cI; 
      // The abstract key is readonly.
      // cA.key = {};
      expect(typeof cA.key).toBe( "number" );
      expect(cA.key).toBe( 3712 );
 
      let cS = new StringCommand( "Hello!" );
      cA = cS;
      expect(typeof cA.key).toBe( "string" );
      expect(cA.key).toBe( "Hello!" );

      let cR = new NamedRecordCommand( new NamedRecord( 3712, "Hello!" ) );
      cA = cR;
      expect(cA.key).toBeInstanceOf( NamedRecord );
      const theKey = cA.key as NamedRecord;

      expect(theKey).not.toEqual( {name: "Hello!", value: 3712} );

      delete theKey[SymCTS];

      expect(theKey).toEqual( {name: "Hello!", value: 3712} );

    });
});


it("violates covariance",() => {

    interface IAbove 
    {
        readonly keyList: Array<{}>;
    }

    class Below implements IAbove {
        public constructor( public readonly keyList: Array<{notAnyObject:boolean}> = [] ) { }
    }  
  
    const b = new Below();
    const a : IAbove = b;
    const shouldNotBeAllowed = {bug:true};
    a.keyList.push(shouldNotBeAllowed);
    expect( b.keyList ).toContain(shouldNotBeAllowed);
});


it("requires explicit covariance support via readonly collections",() => {

    interface ISafeAbove 
    {
        readonly keyList: ReadonlyArray<{}>;
    }
    
    class Below2 implements ISafeAbove {
        public constructor( public readonly keyList: Array<{notAnyObject:boolean}> = [] )
        {
        }
    }  
  
    const b = new Below2();
    const a : ISafeAbove = b;
    const shouldNotBeAllowed = {bug:true};
    //a.keyList.push(shouldNotBeAllowed);
    expect( b.keyList ).not.toContain(shouldNotBeAllowed);
});