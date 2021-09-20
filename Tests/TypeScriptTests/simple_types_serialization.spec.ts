import { Simple, SimpleEnum } from './Output/Json/simple_types_serialization'

test('SimpleEnum serializarion.', () => {
    const a = new Simple();
    a.name = 'n';
    a.power = 7897;
    a.value = SimpleEnum.Three;
    var s = JSON.stringify( a );
});
