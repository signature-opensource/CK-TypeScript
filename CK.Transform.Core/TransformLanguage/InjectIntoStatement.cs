namespace CK.Transform.TransformLanguage;

public sealed class InjectIntoStatement : TransformStatement
{
    public InjectIntoStatement( RawString content, InjectionPoint target )
    {
        Content = content;
        Target = target;
    }

    public RawString Content { get; }

    public InjectionPoint Target { get; }
}
