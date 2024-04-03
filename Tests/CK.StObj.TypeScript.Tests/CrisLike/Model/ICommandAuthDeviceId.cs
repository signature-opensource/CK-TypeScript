using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.CrisLike
{
    /// <summary>
    /// Extends the basic <see cref="ICommandAuthUnsafe"/> to add the <see cref="DeviceId"/> field.
    /// </summary>
    [CKTypeDefiner]
    public interface ICommandAuthDeviceId : ICommandAuthUnsafe
    {
        /// <summary>
        /// Gets or sets the device identifier.
        /// </summary>
        [AmbientValue]
        string? DeviceId { get; set; }
    }
}
