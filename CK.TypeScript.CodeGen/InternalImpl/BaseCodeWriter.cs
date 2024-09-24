using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace CK.TypeScript.CodeGen;

class BaseCodeWriter : ITSCodeWriter
{
    /// <summary>
    /// Heterogeneous list of BaseCodeWriter and string.
    /// </summary>
    internal readonly List<object> _content;
    Dictionary<object, object?>? _memory;

    public BaseCodeWriter( TypeScriptFile f )
    {
        File = f;
        _content = new List<object>();
    }

    public TypeScriptFile File { get; }

    public virtual bool IsEmpty
    {
        get
        {
            if( _content.Count > 0 )
            {
                foreach( var c in _content )
                {
                    if( c is BaseCodeWriter p )
                    {
                        if( !p.IsEmpty ) return false;
                    }
                    else
                    {
                        if( !string.IsNullOrWhiteSpace( Unsafe.As<string>( c ) ) )
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }

    public void DoAdd( string? code )
    {
        if( !String.IsNullOrEmpty( code ) ) _content.Add( code );
    }

    internal virtual void Clear()
    {
        _memory?.Clear();
        _content.Clear();
    }

    internal virtual void Build( ref SmarterStringBuilder b )
    {
        foreach( var c in _content )
        {
            if( c is BaseCodeWriter p ) p.Build( ref b );
            else b.Append( Unsafe.As<string>( c ) );
        }
    }

    public StringBuilder Build( StringBuilder b, bool closeScope )
    {
        var sB = new SmarterStringBuilder( b );
        Build( ref sB );
        return b;
    }

    public IDictionary<object, object?> Memory => _memory ??= new Dictionary<object, object?>();

    public override string ToString() => Build( new StringBuilder(), false ).ToString();
}
