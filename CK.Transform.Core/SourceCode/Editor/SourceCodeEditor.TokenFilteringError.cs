using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Transform.Core;

public sealed partial class SourceCodeEditor
{
    internal sealed class TokenFilteringError
    {
        readonly LinkedTokenFilterBuilderContext _culprit;
        readonly string _message;

        public TokenFilteringError( LinkedTokenFilterBuilderContext culprit, string message )
        {
            _culprit = culprit;
            _message = message;
        }

        public string Message => _message;

        public void Dump( IActivityMonitor monitor )
        {
            using( monitor.OpenError( $"Details of: {_message}" ) )
            {
                var b = new StringBuilder();
                DumpFilterResults( monitor, _culprit, b );
            }
        }

        static void DumpFilterResults( IActivityMonitor monitor,
                                       LinkedTokenFilterBuilderContext c,
                                       StringBuilder b )
        {
            if( c.IsRoot ) return;
            DumpFilterResults( monitor, c.Previous, b );
            b.Clear();
            b.Append( "Filter n°" ).Append( c.Index );
            c.Provider.Describe( b, false );
            b.Append( ": " );
            if( c.IsTransparent )
            {
                b.Append( "(transparent)" );
                monitor.Trace( b.ToString() );
            }
            else
            {
                int eachCount = c.Tokens.Count();
                if( eachCount == 0 ) b.Append( "[Empty]" );
                else if( eachCount == 1 ) b.Append( "[sinle each group]" );
                else b.Append( '[' ).Append( eachCount ).Append( "each groups]" );

                if( eachCount == 0 )
                {
                    monitor.Trace( b.ToString() );
                }
                else
                {
                    using( monitor.OpenTrace( b.ToString() ) )
                    {
                        if( eachCount > 1 )
                        {
                            int eachNumber = 0;
                            foreach( var each in c.Tokens )
                            {
                                DumpRanges( monitor, $"Each group n°{eachNumber}: ", each, b );
                                ++eachNumber;
                            }
                        }
                        else
                        {
                            DumpRanges( monitor, $"Ranges: ", c.Tokens.First(), b );
                        }
                    }
                }
            }

            static void DumpRanges( IActivityMonitor monitor,
                                    string prefix,
                                    IEnumerable<IEnumerable<SourceToken>> each,
                                    StringBuilder b )
            {
                var rangeCount = each.Count();
                if( rangeCount == 0 )
                {
                    monitor.Trace( $"{prefix}Empty." );
                }
                else
                {
                    using( monitor.OpenTrace( $"{prefix}{rangeCount} ranges." ) )
                    {
                        b.Clear();
                        int iRange = 0;
                        foreach( var r in each )
                        {
                            b.Append( "--- (range n°" ).Append( ++iRange ).AppendLine( ") ---" );
                            r.Select( t => t.Token ).Write( b ).AppendLine();
                            b.AppendLine( "---" );
                        }
                        monitor.Trace( b.ToString() );
                    }
                }
            }

        }

    }
}
