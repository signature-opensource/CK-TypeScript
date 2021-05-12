using System;
using System.Collections.Generic;
using System.Text;

#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved

namespace CK.StObj.TypeScript.Tests.CrisLike
{

    /// <summary>
    /// Defines the <see cref="ActorId"/> field.
    /// This is the most basic command part that can be used to authenticate a command in <see cref="AuthLevel.Normal"/>
    /// or <see cref="AuthLevel.Critical"/> authentication levels.
    /// </summary>
    public interface ICommandAuthenticated : ICommandPart
    {
        /// <summary>
        /// Gets or sets the actor identifier.
        /// </summary>
        int ActorId { get; set; }
    }
}
