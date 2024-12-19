using CK.Core;
using CK.Transform.Core;
using System.Collections.Immutable;

namespace CK.Transform.TransformLanguage;

public sealed class InjectIntoStatementOld : CompositeNode, ITransformStatement
{
    static readonly RequiredChild<TokenNode> _injecT = new( 0 );
    static readonly RequiredChild<RawStringOld> _content = new( 1 );
    static readonly RequiredChild<TokenNode> _intoT = new( 2 );
    static readonly RequiredChild<InjectionPointOld> _target = new( 3 );
    static readonly RequiredChild<TokenNode> _statementTerminator = new( 4 );

    public InjectIntoStatementOld( TokenNode inject, RawStringOld content, TokenNode into, InjectionPointOld target, TokenNode terminator )
        : base( inject, content, into, target, terminator )
    {
    }

    InjectIntoStatementOld( ImmutableArray<Trivia> leading, CompositeNodeMutator content, ImmutableArray<Trivia> trailing )
        : base( leading, content, trailing )
    {
    }

    protected override void DoCheckInvariants( int storeLength )
    {
        Throw.CheckArgument( storeLength == 5 );
        _injecT.Check( this, nameof( Inject ) );
        _content.Check( this, nameof( Content ) );
        _intoT.Check( this, nameof( Into ) );
        _target.Check( this, nameof( Target ) );
        _statementTerminator.Check( this, nameof( StatementTerminator ) );
    }

    public TokenNode Inject => _injecT.Get(this);

    public RawStringOld Content => _content.Get(this);

    public TokenNode Into => _intoT.Get(this);

    public InjectionPointOld Target => _target.Get(this);

    public TokenNode? StatementTerminator => _statementTerminator.Get(this);

    internal protected override AbstractNode DoClone( ImmutableArray<Trivia> leading, CompositeNodeMutator content, ImmutableArray<Trivia> trailing )
    {
        return new InjectIntoStatementOld( leading, content, trailing );
    }
}
