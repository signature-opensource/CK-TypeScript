using CK.Core;

namespace CK.CrisLike;

/// <summary>
/// Extends the basic <see cref="ICommandAuthUnsafe"/> to add the <see cref="DeviceId"/> field.
/// </summary>
[CKTypeDefiner]
public interface ICommandAuthDeviceId : ICommandAuthUnsafe
{
    /// <summary>
    /// Gets or sets the device identifier.
    /// </summary>
    [UbiquitousValue]
    string DeviceId { get; set; }
}
