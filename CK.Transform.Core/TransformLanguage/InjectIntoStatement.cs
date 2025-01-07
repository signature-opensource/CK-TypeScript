using CK.Core;
using CK.Transform.Core;

namespace CK.Transform.TransformLanguage;

public sealed class InjectIntoStatement : TransformStatement
{
    public InjectIntoStatement( int beg, int end, RawString content, InjectionPoint target )
        : base( beg, end )
    {
        Content = content;
        Target = target;
    }

    public RawString Content { get; }

    public InjectionPoint Target { get; }

    public override void Apply( IActivityMonitor monitor, SourceCodeEditor code )
    {
        throw new System.NotImplementedException();
    }
}
