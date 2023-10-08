describe('An actual test...', () => {
  it('has always the environment variables set.', () => {
    // The STOBJ_TYPESCRIPT_ENGINE is always "true".
    expect(process.env.STOBJ_TYPESCRIPT_ENGINE).toBe("true");
    expect(process.env.SET_BY_THE_UNIT_TEST).toBe("YES!");
  });
});

// Conditionally skips a test.
const whenCalledFromNetTest = process.env.STOBJ_TYPESCRIPT_ENGINE ? describe : describe.skip;
whenCalledFromNetTest('environment variables set by .Net test', () => {
  it('must be available', () => {
    expect(process.env.SET_BY_THE_UNIT_TEST).toBe("YES!");
  });
});
