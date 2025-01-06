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
}
