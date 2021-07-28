using System;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.StObj.TypeScript.Tests.EmptyCodeGeneratorTypeSample
{
    /// <summary>
    /// This specializes the base <see cref="TypeScriptAttribute"/> to generate an empty TS type.
    /// This is easy to implement since we don't need to manage type imports and code generation
    /// propagation.
    /// When applied to an enum, this preempts the default code generation: the enum will be empty!
    /// </summary>
    [AttributeUsage( AttributeTargets.Class|AttributeTargets.Interface|AttributeTargets.Struct|AttributeTargets.Enum)]
    public class EmptyTypeScriptAttribute : TypeScriptAttribute
    {
        public EmptyTypeScriptAttribute()
            : base( "CK.StObj.TypeScript.Tests.EmptyCodeGeneratorTypeSample.EmptyTypeScriptAttributeImpl, CK.StObj.TypeScript.Tests" )
        {
        }
    }
}
