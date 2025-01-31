using CK.Core;
using CK.TypeScript.Engine;
using System;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.TypeScript.Tests;

public class SpecializedPackageAttributeImpl : TypeScriptPackageAttributeImpl
{
    public SpecializedPackageAttributeImpl( IActivityMonitor monitor, TypeScriptPackageAttribute attr, Type type )
        : base( monitor, attr, type )
    {
    }
}
