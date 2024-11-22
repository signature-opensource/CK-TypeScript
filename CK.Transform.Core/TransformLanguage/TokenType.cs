namespace CK.Transform;

public enum TokenType
{
    TransformClassNumber = Core.TokenType.MaxClassNumber - 1,
    TransformClassBit = 1 << TransformClassNumber,
    TransformClassMask = -1 << (31 - TransformClassNumber),

    Inject = TransformClassBit | 1,
    Into = TransformClassBit | 2,

    /// <summary>
    /// See <see cref="RawString"/>.
    /// </summary>
    RawString = TransformClassBit | 3,
}
