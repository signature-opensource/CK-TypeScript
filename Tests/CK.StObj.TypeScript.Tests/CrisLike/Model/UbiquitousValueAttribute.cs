using System;

namespace CK.CrisLike
{
    /// <summary>
    /// Decorates a <see cref="IAbstractCommand"/> property that must be nullable: the property
    /// must be declared in <see cref="IUbiquitousValues"/>.
    /// </summary>
    [AttributeUsage( AttributeTargets.Property )]
    public sealed class UbiquitousValueAttribute : Attribute
    {
    }

}
