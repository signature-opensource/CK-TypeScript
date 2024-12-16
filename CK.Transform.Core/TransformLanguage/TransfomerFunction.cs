using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Immutable;

namespace CK.Transform.TransformLanguage;

public sealed class TransfomerFunction : CompositeNode
{
    static readonly RequiredChild<TokenNode> _createT = new( 0 );
    static readonly RequiredChild<TokenNode> _languageName = new( 1 );
    static readonly RequiredChild<TokenNode> _transformerT = new( 2 );
    static readonly OptionalChild<TokenNode> _functionName = new( 3 );
    static readonly OptionalChild<InjectionPoint> _onT = new( 4 );
    static readonly OptionalChild<AbstractNode> _target = new( 5 );
    static readonly OptionalChild<TokenNode> _asT = new( 6 );
    static readonly RequiredChild<TokenNode> _beginT = new( 7 );
    static readonly RequiredChild<NodeList<ITransformStatement>> _statements = new( 8 );
    static readonly RequiredChild<TokenNode> _endT = new( 9 );

    string? _name;
    string? _externalName;

    protected override void DoCheckInvariants( int storeLength )
    {
        Throw.CheckArgument( storeLength == 10 );
        _createT.Check( this, "create" );
        _languageName.Check( this, nameof(LanguageName) );
        _transformerT.Check( this, "transfom" );
        _functionName.Check( this, nameof( Name ) );
        _onT.Check( this, "on" );
        _target.Check( this, nameof( Target ) );
        _asT.Check( this, "as" );
        _beginT.Check( this, "begin" );
        _statements.Check( this, nameof(Statements) );
        _endT.Check( this, "end" );
    }

    TransfomerFunction( ImmutableArray<Trivia> leading, CompositeNodeMutator content, ImmutableArray<Trivia> trailing )
        : base( leading, content, trailing )
    {
    }

    public TransfomerFunction( TokenNode create, TokenNode language, TokenNode transformer, TokenNode? functionName, TokenNode? on, AbstractNode? target, TokenNode? asT, TokenNode begin, NodeList<ITransformStatement> statements, TokenNode end )
        : base( create, language, transformer, functionName, on, target, asT, begin, statements, end )
    {
    }

    protected internal override AbstractNode DoClone( ImmutableArray<Trivia> leading, CompositeNodeMutator content, ImmutableArray<Trivia> trailing )
    {
        return new TransfomerFunction( leading, content, trailing );
    }

    /// <summary>
    /// Gets this transformer function name.
    /// Defaults to the empty string.
    /// </summary>
    public string Name => _name ??= _functionName.Get( this )?.Text.ToString() ?? "";

    /// <summary>
    /// Gets the language name.
    /// </summary>
    public ReadOnlySpan<char> LanguageName => _languageName.Get( this ).Text.Span;

    public TransfomerFunction SetName( string? name )
    {
        if( string.IsNullOrEmpty( name ) ) name = "";
        var n = Name;
        if( n == name ) return this;
        var mutator = CreateMutator();
        if( name.Length == 0 )
        {
            mutator.RawItems[_onT.Index] = null;
            mutator.RawItems[_functionName.Index] = null;
        }
        else
        {
            var on = _onT.Get( this );
            if( on == null )
            {
                mutator.RawItems[_onT.Index] = new TokenNode( NodeType.GenericIdentifier, "on".AsMemory(), Analyzer.OneSpace );
                mutator.RawItems[_functionName.Index] = new TokenNode( NodeType.GenericIdentifier, name.AsMemory(), Analyzer.OneSpace );
            }
            else
            {
                var current = _functionName.Get( this );
                Throw.DebugAssert( current != null );
                mutator.RawItems[_functionName.Index] = new TokenNode( current.NodeType, name.AsMemory(), current.LeadingTrivias, current.TrailingTrivias );
            }
        }
        return (TransfomerFunction)mutator.Clone();
    }

    /// <summary>
    /// Gets the target address or name if it is specified.
    /// <para>
    /// This is either a single-line <see cref="RawString"/> or a <see cref="TokenNode"/> without whitespace.
    /// </para>
    /// </summary>
    public AbstractNode? Target => _target.Get( this );

    /// <summary>
    /// Gets the transform statements.
    /// </summary>
    public NodeList<ITransformStatement> Statements => _statements.Get( this );
}
