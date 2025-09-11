using System.Runtime.CompilerServices;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.TypeScript.Tests;

public sealed class SpecializedPackageAttribute : TypeScriptPackageAttribute
{
    public SpecializedPackageAttribute( [CallerFilePath] string? callerFilePath = null )
        : base( "CK.TypeScript.Tests.SpecializedPackageAttributeImpl, CK.TypeScript.Tests", callerFilePath )
    {
    }
}
