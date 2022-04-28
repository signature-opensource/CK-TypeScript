import { Simple, SimpleEnum } from './Output/Json/simple_types_serialization'

test('SimpleEnum serializarion.', () => {
    const a = Simple.create( 7897, 'n', SimpleEnum.Three );

    var s = JSON.stringify( a );
});
