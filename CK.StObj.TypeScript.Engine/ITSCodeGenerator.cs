using CK.Core;
using CK.StObj.TypeScript;
using CK.StObj.TypeScript.Engine;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Global Type Script code generator.
    /// This can be used to generate code for a set of types without the need of explicit attributes
    /// or any independent TypeScript code.
    /// <para>
    /// The <see cref="ITSCodeGeneratorType"/> should be used if the TypeScript generation can be driven
    /// by an attribute on a Type.
    /// </para>
    /// <para>
    /// A global code generator like this coexists with other global and other <see cref="ITSCodeGeneratorType"/> on a type.
    /// It is up to the implementation to handle the collaboration (or to raise an error).
    /// </para>
    /// </summary>
    public interface ITSCodeGenerator
    {
        /// <summary>
        /// Configures the <see cref="TypeScriptAttribute"/> that will be used
        /// by <see cref="TypeScriptGenerator.GetTSTypeFile(Type)"/> to create the file for a given type.
        /// </summary>
        /// <param name="a">The attribute to configure. May be an empty one or the existing attribute on the type.</param>
        void ConfigureTypeScriptAttribute( TypeScriptAttribute a, Type type );

        /// <summary>
        /// Generates the TypeScript code. The <paramref name="generator"/> exposes the current <see cref="TypeScriptGenerator.BinPath"/>.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="generator">The configured code generator to use.</param>
        /// <returns>True on success, false on error (errors must be logged).</returns>
        bool GenerateCode( IActivityMonitor monitor, TypeScriptGenerator generator );

    }
}
