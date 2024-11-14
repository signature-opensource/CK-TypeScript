
describe('An actual test...', () => {
  it('has always the environment variables set.', () => {
    // The CK_TYPESCRIPT_ENGINE is always "true".
    expect( CKTypeScriptEnv.CK_TYPESCRIPT_ENGINE).toBe("true");
    expect( CKTypeScriptEnv.SET_BY_THE_UNIT_TEST).toBe("YES!");
  });
});

// Conditionally skips a test.
const whenCalledFromNetTest = CKTypeScriptEnv.SET_BY_THE_UNIT_TEST ? describe : describe.skip;

whenCalledFromNetTest('environment variables set by .Net test', () => {
  it('must be available', () => {
      expect( CKTypeScriptEnv.SET_BY_THE_UNIT_TEST ).toBe("YES!");
  });
});
