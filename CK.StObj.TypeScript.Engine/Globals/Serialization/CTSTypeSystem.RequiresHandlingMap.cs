using CK.Core;
using System;
using System.Collections;

namespace CK.Setup
{
    public sealed partial class CTSTypeSystem
    {
        readonly struct RequiresHandlingMap
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

            internal RequiresHandlingMap( IPocoTypeSet exchangeSet )
            {
                _exchangeSet = exchangeSet;
                _done = new TypeSet( exchangeSet.TypeSystem.AllNonNullableTypes.Count );
                _result = new TypeSet( exchangeSet.TypeSystem.AllNonNullableTypes.Count );
            }

            public IPocoTypeSet ExchangeSet => _exchangeSet;

            public bool RequiresToJSONCall( IPocoType t )
            {
                // Our Guid and Extended/NormalizedCultureInfo and SimpleUserMessage have toJSON().
                // Luxon DateTime and Duration also have it. But... we use Luxon DateTime toJSON as
                // it uses the ISO 8601 format (same as .Net) but we don't use the Duration.toJSON()
                // because it also uses the ISO 8601 and this is not supported on .NET:
                // see https://github.com/dotnet/runtime/issues/28862#issuecomment-1273503317
                var tt = t.Type;
                return tt == typeof( Guid )
                       || tt == typeof( DateTime ) || tt == typeof( DateTimeOffset )
                       || tt == typeof( ExtendedCultureInfo ) || tt == typeof( NormalizedCultureInfo )
                       || tt == typeof( SimpleUserMessage );
            }

            public bool Contains( IPocoType t )
            {
                if( _done.Add( t ) )
                {
                    Throw.CheckArgument( ExchangeSet.Contains( t ) );
                    if( t.IsPolymorphic || RequiresToJSONCall( t ) || t.Type == typeof(TimeSpan) )
                    {
                        _result.Add( t );
                    }
                    else
                    {
                        if( t is ICollectionPocoType coll )
                        {
                            if( coll.Kind is PocoTypeKind.List or PocoTypeKind.Array )
                            {
                                // Array or List of cool types are cool.
                                if( Contains( coll.ItemTypes[0] ) )
                                {
                                    _result.Add( t );
                                }
                            }
                            else
                            {
                                // Set and Map require to be handled.
                                _result.Add( t );
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
