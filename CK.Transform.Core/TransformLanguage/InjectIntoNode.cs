using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace CK.Transform;

using CNode = SNode<
        TokenNode, // inject
        RawString,
        TokenNode, // into
        RawString,
        TokenNode>; // ;

public sealed class InjectIntoNode : SyntaxNode
{
    readonly CNode _content;

    public InjectIntoNode( TokenNode inject, RawString content, TokenNode into, RawString target, TokenNode terminator )
        : base()
    {
        _content = new CNode( inject, content, into, target, terminator );
    }

    InjectIntoNode( InjectIntoNode o, ImmutableArray<Trivia> leading, IEnumerable<AbstractNode>? content, ImmutableArray<Trivia> trailing )
        : base( leading, trailing )
    {
        if( content == null ) _content = o._content;
        else
        {
            _content = new CNode( content );
        }
    }

    public override IReadOnlyList<AbstractNode> ChildrenNodes => _content;

    public override IList<AbstractNode> GetRawContent() => _content.GetRawContent();

    public TokenNode InjectT => _content.V1;

    public RawString Content => _content.V2;

    public TokenNode IntoT => _content.V3;

    public RawString Target => _content.V4;

    public TokenNode StatementTerminator => _content.V5;


    protected override AbstractNode DoClone( ImmutableArray<Trivia> leading, IList<AbstractNode>? content, ImmutableArray<Trivia> trailing )
    {
        return new InjectIntoNode( this, leading, content, trailing );
    }
}
