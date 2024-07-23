using CK.Setup;
using System;

namespace CK.StObj.TypeScript
{
    /// <summary>
    /// Required attribute for <see cref="TypeScriptPackage"/>.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
    public class TypeScriptPackageAttribute : ContextBoundDelegationAttribute
    {
        /// <summary>
        /// Initializes a new <see cref="TypeScriptPackageAttribute"/>.
        /// </summary>
        public TypeScriptPackageAttribute()
            : base( "CK.StObj.TypeScript.Engine.TypeScriptPackageAttributeImpl, CK.StObj.TypeScript.Engine" )
        {
        }

        /// <summary>
        /// Initializes a new specialized <see cref="TypeScriptPackageAttribute"/>.
        /// </summary>
        protected TypeScriptPackageAttribute( string actualAttributeTypeAssemblyQualifiedName )
            : base( actualAttributeTypeAssemblyQualifiedName )
        {
        }

        /// <summary>
        /// Gets or sets the package to which this package belongs.
        /// </summary>
        public Type? Package { get; set; }
    }

}
