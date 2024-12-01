using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace CK.Transform.TransformLanguage;

public sealed class InjectIntoNode : CompositeNode
{
    static readonly RequiredChild<TokenNode> _injecT = new( 0 );
    static readonly RequiredChild<RawString> _content = new( 1 );
    static readonly RequiredChild<TokenNode> _intoT = new( 2 );
    static readonly RequiredChild<RawString> _target = new( 3 );
    static readonly OptionalChild<TokenNode> _statementTerminator = new( 4 );

    public InjectIntoNode( TokenNode injectT, RawString content, TokenNode intoT, RawString target, TokenNode? terminator )
        : base( [], [], injectT, content, intoT, target, terminator )
    {
    }

    InjectIntoNode( ImmutableArray<Trivia> leading, CompositeNodeMutator content, ImmutableArray<Trivia> trailing )
        : base( leading, content, trailing )
    {
    }

    protected override void DoCheckInvariants( int storeLength )
    {
        Throw.CheckArgument( storeLength == 5 );
        _injecT.Check( this, nameof( InjectT ) );
        _content.Check( this, nameof( Content ) );
        _intoT.Check( this, nameof( IntoT ) );
        _target.Check( this, nameof( Target ) );
        _statementTerminator.Check( this, nameof( StatementTerminator ) );
    }

    public TokenNode InjectT => _injecT.Get(this);

    public RawString Content => _content.Get(this);

    public TokenNode IntoT => _intoT.Get(this);

    public RawString Target => _target.Get(this);

    public TokenNode? StatementTerminator => _statementTerminator.Get(this);

    internal protected override AbstractNode DoClone( ImmutableArray<Trivia> leading, CompositeNodeMutator content, ImmutableArray<Trivia> trailing )
    {
        return new InjectIntoNode( leading, content, trailing );
    }

    internal static IAbstractNode Create( TokenNode inject, IAbstractNode content, IAbstractNode into, IAbstractNode target, IAbstractNode terminator )
    {
        if( content is RawString safeContent
            && into)
    }
}
