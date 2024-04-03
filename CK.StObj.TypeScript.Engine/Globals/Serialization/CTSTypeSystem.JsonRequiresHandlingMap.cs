using CK.Core;
using System;
using System.Collections;
using System.Numerics;

namespace CK.Setup
{
    public sealed partial class CTSTypeSystem
    {
        readonly struct JsonRequiresHandlingMap
        {
            sealed class BitArrayTypeSet
            {
                readonly BitArray _flags;

                public BitArrayTypeSet( int nonNullableCount ) => _flags = new BitArray( nonNullableCount );

                public bool Contains( IPocoType t ) => _flags[t.Index >> 1];

                public bool Add( IPocoType t )
                {
                    if( _flags[t.Index >> 1] ) return false;
                    _flags[t.Index >> 1] = true;
                    return true;
                }
            }

            readonly BitArrayTypeSet _done;
            readonly BitArrayTypeSet _result;
            readonly IPocoTypeSet _exchangeSet;

            internal JsonRequiresHandlingMap( IPocoTypeSet exchangeSet )
            {
                _exchangeSet = exchangeSet;
                _done = new BitArrayTypeSet( exchangeSet.TypeSystem.AllNonNullableTypes.Count );
                _result = new BitArrayTypeSet( exchangeSet.TypeSystem.AllNonNullableTypes.Count );
            }

            public IPocoTypeSet ExchangeSet => _exchangeSet;

            public bool HasToJSONMethod( IPocoType t )
            {
                // Our Guid and Extended/NormalizedCultureInfo and SimpleUserMessage have toJSON().
                // Luxon DateTime and Duration also have it. But... we use Luxon DateTime toJSON as
                // it uses the ISO 8601 format (same as .Net) but we don't use the Duration.toJSON()
                // because it also uses the ISO 8601 and this is not supported on .NET:
                // see https://github.com/dotnet/runtime/issues/28862#issuecomment-1273503317
                // (And frankly, the ISO duration is a PITA. See https://github.com/moment/luxon/issues/1514 for instance.)
                // The TimeSpan is handled explicitly (from .Net 10th of microseconds to milliseconds).
                var tt = t.Type;
                return tt == typeof( Guid )
                       || tt == typeof( Decimal )
                       || tt == typeof( DateTime ) || tt == typeof( DateTimeOffset )
                       || tt == typeof( ExtendedCultureInfo ) || tt == typeof( NormalizedCultureInfo )
                       || tt == typeof( SimpleUserMessage );
            }

            public bool Contains( IPocoType t )
            {
                if( _done.Add( t ) )
                {
                    Throw.CheckArgument( ExchangeSet.Contains( t ) );
                    if( t.IsPolymorphic
                        || (t.Kind == PocoTypeKind.Basic
                            && (t.Type == typeof(TimeSpan)
                                || t.Type == typeof( long )
                                || t.Type == typeof( ulong )
                                || t.Type == typeof( BigInteger ))) )
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
                        else if( t is IRecordPocoType r )
                        {
                            // For records (like for Primary poco), as soon as a field's type must be handled,
                            // then the record must be handled.
                            // Anonymous records with fields names use {"object":"syntax"}, they must also be handled
                            // to be returned as ["tuple",'syntax"].
                            bool hasFieldNames = false;
                            foreach( var f in r.Fields )
                            {
                                if( !_exchangeSet.Contains( f.Type ) ) continue;
                                hasFieldNames |= !f.IsUnnamed;
                                if( Contains( f.Type ) )
                                {
                                    _result.Add( t );
                                    break;
                                }
                            }
                            if( r.IsAnonymous && hasFieldNames )
                            {
                                _result.Add( t );
                            }
                        }
                        else if( t is IPrimaryPocoType p )
                        {
                            foreach( var f in p.Fields )
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
