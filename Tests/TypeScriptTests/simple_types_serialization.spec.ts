import { IWillBeEmpty } from './Output/Json/simple_types_serialization/IWillBeEmpty'

test('interface IWillBeEmpty is empty.', () => {
  const b = {a:null};
  const a : IWillBeEmpty = b;
  Object.keys(a).length === 0;
  });