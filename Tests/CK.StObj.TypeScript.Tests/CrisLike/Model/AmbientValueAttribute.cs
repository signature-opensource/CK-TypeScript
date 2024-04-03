
#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved

using System;

namespace CK.CrisLike
{
    /// <summary>
    /// Decorates a <see cref="IPoco"/> property that must be nullable: the property
    /// must be declared in <see cref="AmbientValues.IAmbientValues"/>.
    /// </summary>
    [AttributeUsage( AttributeTargets.Property )]
    public sealed class AmbientValueAttribute : Attribute
    {
    }

}
