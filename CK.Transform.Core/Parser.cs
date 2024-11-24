using System;

namespace CK.Transform.Core;

public sealed class Parser<T> : IParser where T : Analyzer, new()
{
    readonly Analyzer _analyzer;
    readonly string _name;

    public Parser()
        : this( typeof(T).Name.Replace("Analyzer","") )
    {
    }

    public Parser( string name )
    {
        _analyzer = new T();
        _name = name;
    }

    public IAbstractNode Parse( ref ReadOnlyMemory<char> text )
    {
        _analyzer.Reset( text );
        return _analyzer.Parse();
    }

    public override string ToString() => _name;
}
