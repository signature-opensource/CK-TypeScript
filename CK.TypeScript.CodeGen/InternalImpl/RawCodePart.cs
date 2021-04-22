using System;
using System.Collections.Generic;
using System.Text;

namespace CK.TypeScript.CodeGen
{
    class RawCodePart : BaseCodeWriter, ITSCodePart
    {
        Dictionary<string, NamedCodePart>? _namedParts;

        internal RawCodePart( string closer )
        {
            Closer = closer;
        }

        public string Closer { get; }

        public ITSNamedCodePart CreateNamedPart( string name, string closer, bool top = false ) => DoCreate( name, closer, top );

        public ITSCodePart CreatePart( string closer = "", bool top = false )
        {
            var p = new RawCodePart( closer );
            if( top ) Parts.Insert( 0, p );
            else Parts.Add( p );
            return p;
        }

        public ITSNamedCodePart? FindNamedPart( string name ) => _namedParts?.GetValueOrDefault( name );

        public ITSNamedCodePart FindOrCreateNamedPart( string name, string? closer = null, bool top = false )
        {
            if( _namedParts != null && _namedParts.TryGetValue( name, out var p ) )
            {
                if( closer != null && p.Closer != closer )
                {
                    throw new ArgumentException( $"Existing named part Closer is '{p.Closer}' whereas closer parameter is '{closer}'.", nameof(closer) );
                }
                return p;
            }
            return DoCreate( name, closer ?? String.Empty, top );
        }

        ITSNamedCodePart DoCreate( string name, string closer, bool top )
        {
            if( _namedParts == null ) _namedParts = new Dictionary<string, NamedCodePart>();
            var p = new NamedCodePart( name, closer );
            _namedParts.Add( name, p );
            if( top ) Parts.Insert( 0, p );
            else Parts.Add( p );
            return p;
        }

        internal override SmarterStringBuilder Build( SmarterStringBuilder b )
        {
            Build( b, true );
            return b;
        }

        public void Build( SmarterStringBuilder b, bool closeScope )
        {
            base.Build( b );
            if( closeScope && Closer.Length != 0 ) b.AppendLine().Append( Closer );
        }

    }
}
