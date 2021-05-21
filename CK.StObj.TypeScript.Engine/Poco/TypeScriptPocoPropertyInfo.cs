using CK.Setup;
using CK.TypeScript.CodeGen;
using System.Linq;

namespace CK.StObj.TypeScript.Engine
{
    /// <summary>
    /// Exposed by the <see cref="PocoGeneratingEventArgs"/> so that generation
    /// can be altered.
    /// </summary>
    public class TypeScriptPocoPropertyInfo
    {
        internal TypeScriptPocoPropertyInfo( IPocoPropertyInfo p,
                                             string propType,
                                             string propName,
                                             string paramName,
                                             string? propComment,
                                             string? paramComment )
        {
            PocoProperty = p;
            Property = new TypeScriptVarType( propName, propType );
            Property.Comment = propComment;
            Property.Optional = p.IsEventuallyNullable;
            if( p.AutoInstantiated )
            {
                Property.DefaultValue = "new " + propType + "()";
            }
            ParameterName = paramName;
            ParameterComment = paramComment;
        }

        /// <summary>
        /// Gets the poco property info.
        /// </summary>
        public IPocoPropertyInfo PocoProperty { get; }

        /// <summary>
        /// Gets the property description.
        /// </summary>
        public TypeScriptVarType Property { get; }

        /// <summary>
        /// Gets or sets the parameter name for this property.
        /// This is the <see cref="IPocoPropertyInfo.PropertyName"/> in camel case.
        /// </summary>
        public string ParameterName { get; }

        /// <summary>
        /// Gets the comments to use in TypeScript for the parameter that corresponds to the property.
        /// This is the comment in C# without the "Gets or sets " prefix if any.
        /// </summary>
        public string? ParameterComment { get; }

        /// <summary>
        /// Gets or sets the code that assigns the <see cref="Property"/> from the parameters
        /// in the generated <c>create</c> method.
        /// The newly created command is the variable <c>c</c>.
        /// <para>
        /// When null (the default), "c.propertyName = parameterName;" is used if this <see cref="ParameterName"/> can be found
        /// in the <see cref="TypeScriptPocoClass.CreateParameters"/> list.
        /// This enables more complex expressions (multiple parameters or derivations from other parameters) to be generated.
        /// </para>
        /// </summary>
        public string? OverriddenAssignmentCreateMethodCode { get; set; }
    }

}
