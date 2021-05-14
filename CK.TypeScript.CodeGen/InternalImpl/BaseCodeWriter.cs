using System;
using System.Collections.Generic;
using System.Text;

namespace CK.TypeScript.CodeGen
{
    class BaseCodeWriter : ITSCodeWriter
    {
        /// <summary>
        /// Heterogeneous list of BaseCodeWriter and string.
        /// </summary>
        internal readonly List<object> Content;
        Dictionary<object, object?>? _memory;

        public BaseCodeWriter( TypeScriptFile f )
        {
            File = f;
            Content = new List<object>();
        }

        public TypeScriptFile File { get; }

        public void DoAdd( string? code )
        {
            if( !String.IsNullOrEmpty( code ) ) Content.Add( code );
        }

        internal bool? StartsWith( string prefix )
        {
            foreach( var o in Content )
            {
                if( o is string s )
                {
                    s = s.TrimStart();
                    if( s.Length > 0 ) return s.StartsWith( prefix );
                }
                else
                {
                    bool? r = ((BaseCodeWriter)o).StartsWith( prefix );
                    if( r.HasValue ) return r;
                }
            }
            return null;
        }

        internal virtual SmarterStringBuilder Build( SmarterStringBuilder b )
        {
            b.AppendLine();
            foreach( var c in Content )
            {
                if( c is BaseCodeWriter p ) p.Build( b );
                else b.Append( (string)c );
            }
            b.AppendLine();
            return b;
        }

        public StringBuilder Build( StringBuilder b, bool closeScope ) => Build( new SmarterStringBuilder( b ) ).Builder!;

        public IDictionary<object, object?> Memory => _memory ?? (_memory = new Dictionary<object, object?>());

        public override string ToString() => Build( new SmarterStringBuilder( new StringBuilder() ) ).ToString();
    }
}
