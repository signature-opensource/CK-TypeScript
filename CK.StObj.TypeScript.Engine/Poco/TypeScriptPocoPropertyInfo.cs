using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Diagnostics;
using System.Linq;

namespace CK.StObj.TypeScript.Engine
{
    /// <summary>
    /// Exposed by the <see cref="PocoGeneratingEventArgs"/> so that generation
    /// can be altered.
    /// </summary>
    public class TypeScriptPocoPropertyInfo
    {
        internal TypeScriptPocoPropertyInfo( TypeScriptContext c,
                                             IPrimaryPocoField field,
                                             ITSType fieldType,
                                             string? propComment,
                                             string? paramComment )
        {
            Debug.Assert( field.IsExchangeable );
            Field = field;

            Property = new TypeScriptVarType( c.Root.ToIdentifier( field.Name ), fieldType );
            Property.Comment = propComment;
            CtorParameterName = TypeScriptRoot.ToIdentifier( field.Name, false );
            CtorParameterComment = paramComment;
            CreateMethodParameter = new TypeScriptVarType( CtorParameterName, fieldType )
            {
                Comment = paramComment
            };
        }

        /// <summary>
        /// Gets the poco property info.
        /// </summary>
        public IPrimaryPocoField Field { get; }

        /// <summary>
        /// Gets the property description.
        /// Its <see cref="TypeScriptVarType.DefaultValueSource"/> is handled by the Poco constructor.
        /// </summary>
        public TypeScriptVarType Property { get; }

        /// <summary>
        /// Gets the constructor parameter name for this property.
        /// This is the <see cref="IPocoBasePropertyInfo.PropertyName"/> in camel case.
        /// </summary>
        public string CtorParameterName { get; }

        /// <summary>
        /// Gets the comments to use in TypeScript for the constructor parameter that corresponds to the property.
        /// This is the comment in C# without the "Gets or sets " prefix if any.
        /// </summary>
        public string? CtorParameterComment { get; }

        /// <summary>
        /// Gets or sets the parameter of the create method.
        /// This is initially never null: each property has necessarily a parameter in the create method, but this
        /// can be typically set to null by <see cref="PocoCodeGenerator.PocoGenerating"/> handlers to
        /// remove the parameter.
        /// </summary>
        public TypeScriptVarType? CreateMethodParameter { get; set; }

        /// <summary>
        /// Gets or sets the code that assigns the <see cref="Property"/> from the parameters
        /// in the generated <c>create</c> method.
        /// The newly created command is the variable <c>c</c>.
        /// <para>
        /// When null (the default), "c.propertyName = parameterName;" is used if this <see cref="CtorParameterName"/> can be found
        /// in the <see cref="TypeScriptPocoClass.Properties"/> list.
        /// This enables more complex expressions (multiple parameters or derivations from other parameters) to be generated.
        /// </para>
        /// </summary>
        public string? OverriddenAssignmentCreateMethodCode { get; set; }
    }

}
