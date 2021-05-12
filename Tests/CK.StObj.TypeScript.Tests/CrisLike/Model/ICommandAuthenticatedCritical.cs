using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved

namespace CK.StObj.TypeScript.Tests.CrisLike
{

    /// <summary>
    /// Marker interface for commands that require the <see cref="AuthLevel.Critical"/> level to be validated.
    /// </summary>
    [CKTypeDefiner]
    public interface ICommandAuthenticatedCritical : ICommandAuthenticated
    {
    }
}
