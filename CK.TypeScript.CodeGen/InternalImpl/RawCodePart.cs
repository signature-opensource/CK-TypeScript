using System;
using System.Collections.Generic;
using System.Linq;
using CK.Text;
using System.Text;

namespace CK.TypeScript.CodeGen
{
    class RawCodePart : BaseCodeWriter, ITSCodePart
    {
        Dictionary<object, KeyedCodePart>? _keyedParts;

        internal RawCodePart( TypeScriptFile f, string closer )
            : base( f )
        {
            Closer = closer.NormalizeEOL();
        }

        public string Closer { get; }

        public IEnumerable<ITSCodePart> Parts => Content.OfType<ITSCodePart>();

        public ITSKeyedCodePart CreateKeyedPart( object key, string closer, bool top = false ) => DoCreate( key, closer, top );

        public ITSCodePart CreatePart( string closer = "", bool top = false )
        {
            var p = new RawCodePart( File, closer );
            if( top ) Content.Insert( 0, p );
            else Content.Add( p );
            return p;
        }

        public ITSKeyedCodePart? FindKeyedPart( object key ) => _keyedParts?.GetValueOrDefault( key );

        public ITSKeyedCodePart FindOrCreateKeyedPart( object key, string? closer = null, bool top = false )
        {
            if( _keyedParts != null && _keyedParts.TryGetValue( key, out var p ) )
            {
                if( closer != null && p.Closer != closer.NormalizeEOL() )
                {
                    throw new ArgumentException( $"Existing keyed part Closer is '{p.Closer}' whereas closer parameter is '{closer}' (key is '{key}').", nameof(closer) );
                }
                return p;
            }
            return DoCreate( key, closer ?? String.Empty, top );
        }

        ITSKeyedCodePart DoCreate( object key, string closer, bool top )
        {
            if( _keyedParts == null ) _keyedParts = new Dictionary<object, KeyedCodePart>();
            var p = new KeyedCodePart( File, key, closer );
            _keyedParts.Add( key, p );
            if( top ) Content.Insert( 0, p );
            else Content.Add( p );
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
