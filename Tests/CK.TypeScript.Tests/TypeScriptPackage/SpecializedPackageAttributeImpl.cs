using CK.Core;
using CK.TypeScript.Engine;
using System;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.TypeScript.Tests;

public class SpecializedPackageAttributeImpl : TypeScriptGroupOrPackageAttributeImpl
{
    public SpecializedPackageAttributeImpl( IActivityMonitor monitor, SpecializedPackageAttribute attr, Type type )
        : base( monitor, attr, type )
    {
    }
}
