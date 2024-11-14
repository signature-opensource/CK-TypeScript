using CK.Core;

#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved

namespace CK.CrisLike;

/// <summary>
/// Extends the basic <see cref="ICommandAuthUnsafe"/> to add the <see cref="ActualActorId"/> field.
/// </summary>
[CKTypeDefiner]
public interface ICommandAuthImpersonation : ICommandAuthUnsafe
{
    /// <summary>
    /// Gets or sets the actual actor identifier: the one that is connected, regardless of any impersonation.
    /// The default <see cref="CrisAuthenticationService"/> validates this field against the current <see cref="IAuthenticationInfo.ActualUser"/>.
    /// </summary>
    [UbiquitousValue]
    int ActualActorId { get; set; }
}
