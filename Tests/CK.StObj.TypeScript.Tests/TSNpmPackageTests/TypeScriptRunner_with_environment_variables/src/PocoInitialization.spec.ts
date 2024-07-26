import { WithReadOnly } from "@local/ck-gen";
import { WithUnions } from "@local/ck-gen";

describe('created poco are initialized', () => {
  it('with readonly', () => {
    const p = new WithReadOnly();
    expect( p.list ).toBeDefined();
    expect( p.map).toBeDefined();
    expect( p.set ).toBeDefined();
    expect( p.targetPath ).toBeDefined();
    expect( p.poco ).toBeDefined();
    // ... and this should be initialized!
    expect( p.poco.nullableIntOrString ).toBeUndefined();
    expect( p.poco.intOrStringNoDefault ).toBe( 0 );
    expect( p.poco.intOrStringWithDefault ).toBe( 3712 );
    expect( p.poco.nullableIntOrStringWithDefault ).toBe( 42 );
  });
  it('with simple union types', () => {
    const p = new WithUnions();
    expect( p.nullableIntOrString ).toBeUndefined();
    expect( p.intOrStringNoDefault ).toBe( 0 );
    expect( p.intOrStringWithDefault ).toBe( 3712 );
    expect( p.nullableIntOrStringWithDefault ).toBe( 42 );
  });
});