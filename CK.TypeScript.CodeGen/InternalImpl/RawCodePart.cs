using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.TypeScript.CodeGen
{
    class RawCodePart : BaseCodeWriter, ITSCodePart
    {
        static readonly string _defaultCloser = "}\n".ReplaceLineEndings();
        readonly string _closer;
        Dictionary<object, KeyedCodePart>? _keyedParts;

        internal RawCodePart( TypeScriptFile f, string closer )
            : base( f )
        {
            _closer = NormalizeCloser( closer );
        }

        internal static string NormalizeCloser( string closer )
        {
            if( closer == "}\n" ) return _defaultCloser;
            return closer.ReplaceLineEndings();
        }

        internal override void Clear()
        {
            base.Clear();
            _keyedParts?.Clear();
        }

        public string Closer => _closer;

        public IEnumerable<ITSCodePart> Parts => _content.OfType<ITSCodePart>();

        public ITSKeyedCodePart CreateKeyedPart( object key, string closer, bool top = false ) => DoCreate( key, closer, top );

        public ITSCodePart CreatePart( string closer = "", bool top = false )
        {
            var p = new RawCodePart( File, closer );
            if( top ) _content.Insert( 0, p );
            else _content.Add( p );
            return p;
        }

        public ITSKeyedCodePart? FindKeyedPart( object key ) => _keyedParts?.GetValueOrDefault( key );

        public ITSKeyedCodePart FindOrCreateKeyedPart( object key, string? closer = null, bool top = false )
        {
            if( _keyedParts != null && _keyedParts.TryGetValue( key, out var p ) )
            {
                if( closer != null && p.Closer != closer.ReplaceLineEndings() )
                {
                    throw new ArgumentException( $"Existing keyed part Closer is '{p.Closer}' whereas closer parameter is '{closer}' (key is '{key}').", nameof(closer) );
                }
                return p;
            }
            return DoCreate( key, closer ?? String.Empty, top );
        }

        ITSKeyedCodePart DoCreate( object key, string closer, bool top )
        {
            _keyedParts ??= new Dictionary<object, KeyedCodePart>();
            var p = new KeyedCodePart( File, key, closer );
            _keyedParts.Add( key, p );
            if( top ) _content.Insert( 0, p );
            else _content.Add( p );
            return p;
        }

        internal override SmarterStringBuilder Build( SmarterStringBuilder b ) => Build( b, true );

        public SmarterStringBuilder Build( SmarterStringBuilder b, bool closeScope )
        {
            base.Build( b );
            if( closeScope && Closer.Length != 0 ) b.AppendLine().Append( Closer );
            return b;
        }

    }
}
