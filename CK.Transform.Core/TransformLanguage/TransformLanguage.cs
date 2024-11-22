using CK.Transform.Core;

namespace CK.Transform;

public class TransformLanguage
{
    static TransformLanguage()
    {
        TokenTypeExtensions.ReserveTokenClass( (int)TokenType.TransformClassNumber, nameof(TransformLanguage) );
    }
}
