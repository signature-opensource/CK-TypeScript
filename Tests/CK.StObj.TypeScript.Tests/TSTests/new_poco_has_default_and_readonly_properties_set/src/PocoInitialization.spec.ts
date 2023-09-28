import { WithReadOnly } from "@local/ck-gen";

describe('when creating', () => {
    it('created poco are initialized', () => {
      // this should be new WithReadOnly()...
      const p = WithReadOnly.create();
      expect( p.list ).toBeDefined();
      expect( p.map).toBeDefined();
      expect( p.set ).toBeDefined();
      expect( p.targetPath ).toBeDefined();
      expect( p.poco ).toBeDefined();
      // ... and this should be initialized!
      //expect( p.poco.nonNullableListOrDictionaryOrDouble ).toBeDefined();
      expect( p.poco.nullableIntOrString ).toBeUndefined();
      expect( p.poco.withDefaultValue ).toBe( 3712 );
    });
  });