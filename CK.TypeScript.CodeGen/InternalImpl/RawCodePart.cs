using System;
using System.Collections.Generic;
using System.Text;

namespace CK.TypeScript.CodeGen
{
    class RawCodePart : BaseCodeWriter, ITSCodePart
    {
        Dictionary<object, KeyedCodePart>? _namedParts;

        internal RawCodePart( TypeScriptFile f, string closer )
            : base( f )
        {
            Closer = closer;
        }

        public string Closer { get; }

        public ITSKeyedCodePart CreateKeyedPart( object key, string closer, bool top = false ) => DoCreate( key, closer, top );

        public ITSCodePart CreatePart( string closer = "", bool top = false )
        {
            var p = new RawCodePart( File, closer );
            if( top ) Parts.Insert( 0, p );
            else Parts.Add( p );
            return p;
        }

        public ITSKeyedCodePart? FindKeyedPart( object key ) => _namedParts?.GetValueOrDefault( key );

        public ITSKeyedCodePart FindOrCreateKeyedPart( object key, string? closer = null, bool top = false )
        {
            if( _namedParts != null && _namedParts.TryGetValue( key, out var p ) )
            {
                if( closer != null && p.Closer != closer )
                {
                    throw new ArgumentException( $"Existing named part Closer is '{p.Closer}' whereas closer parameter is '{closer}'.", nameof(closer) );
                }
                return p;
            }
            return DoCreate( key, closer ?? String.Empty, top );
        }

        ITSKeyedCodePart DoCreate( object key, string closer, bool top )
        {
            if( _namedParts == null ) _namedParts = new Dictionary<object, KeyedCodePart>();
            var p = new KeyedCodePart( File, key, closer );
            _namedParts.Add( key, p );
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
