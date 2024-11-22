using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Transform.Core;

public interface IParser
{
    string LangageName { get; }

    IAbstractNode Parse( ref ReadOnlyMemory<char> text );
}

public abstract class Parser<T> : IParser where T : IAbstractNode
{
    public string LangageName => GetType().Name;

    public abstract T Parse( ref ReadOnlyMemory<char> text );

    IAbstractNode IParser.Parse( ref ReadOnlyMemory<char> text ) => Parse( text );
}

public sealed class CompositeParser : IParser<IAbstractNode>
{
    readonly ImmutableArray<IParser> _parsers;
    readonly string _name;

    public string LangageName => _name;

    public CompositeParser( params ImmutableArray<IParser> parsers )
    {
        _parsers = parsers;
        _name = _parsers.Select( p => p.LangageName ).Concatenate( ", " );
    }

    public IAbstractNode Parse( ref ReadOnlyMemory<char> text )
    {

    }
}
