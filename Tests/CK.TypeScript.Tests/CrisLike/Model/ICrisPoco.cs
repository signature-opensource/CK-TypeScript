using CK.Core;

namespace CK.CrisLike;

/// <summary>
/// This abstract interface marker is a simple <see cref="IPoco"/> that tags
/// all the objects managed by Cris.
/// </summary>
[CKTypeSuperDefiner]
public interface ICrisPoco : IPoco
{
    // This is disabled since we do not generate the C# code that implements the
    // CommandModel.
    ///// <summary>
    ///// Gets the <see cref="ICrisPocoModel"/> that describes this Cris poco.
    ///// This property is automatically implemented. 
    ///// </summary>
    //[AutoImplementationClaim]
    //ICrisPocoModel CrisPocoModel { get; }
}
