using CK.Core;
using System.Collections;

namespace CK.Setup
{
    public sealed partial class CTSTypeSystem
    {
        readonly struct HasPolymorphicMap
        {
            sealed class TypeSet
            {
                readonly BitArray _flags;

                public TypeSet( int nonNullableCount ) => _flags = new BitArray( nonNullableCount );

                public bool Contains( IPocoType t ) => _flags[t.Index >> 1];

                public bool Add( IPocoType t )
                {
                    if( _flags[t.Index >> 1] ) return false;
                    _flags[t.Index >> 1] = true;
                    return true;
                }
            }

            readonly TypeSet _done;
            readonly TypeSet _result;
            readonly IPocoTypeSet _exchangeSet;

            internal HasPolymorphicMap( IPocoTypeSet exchangeSet )
            {
                _exchangeSet = exchangeSet;
                _done = new TypeSet( exchangeSet.TypeSystem.AllNonNullableTypes.Count );
                _result = new TypeSet( exchangeSet.TypeSystem.AllNonNullableTypes.Count );
            }

            public IPocoTypeSet ExchangeSet => _exchangeSet;

            public bool Contains( IPocoType t )
            {
                if( _done.Add( t ) )
                {
                    Throw.CheckArgument( ExchangeSet.Contains( t ) );
                    if( t.IsPolymorphic ) _result.Add( t );
                    else
                    {
                        if( t is ICollectionPocoType coll )
                        {
                            foreach( var i in coll.ItemTypes )
                            {
                                if( Contains( i ) )
                                {
                                    _result.Add( t );
                                    break;
                                }
                            }
                        }
                        else if( t is ICompositePocoType c )
                        {
                            foreach( var f in c.Fields )
                            {
                                if( !_exchangeSet.Contains( f.Type ) ) continue;
                                if( Contains( f.Type ) )
                                {
                                    _result.Add( t );
                                    break;
                                }
                            }
                        }
                    }
                }
                return _result.Contains( t );
            }


        }

    }

}
