using CK.Core;
using CK.StObj.TypeScript;
using CK.StObj.TypeScript.Engine;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Type Script code generator for a type. This interface is typically implemented
    /// by delegated attribute classes.
    /// <para>
    /// Whenever an attribute implementation with this interface exists on a type, the implementation of
    /// the <see cref="TypeScriptAttribute"/> (if the attribute exists) does nothing: it is up to this ITSCodeGeneratorType
    /// to generate the Type Script with its <see cref="GenerateCode(IActivityMonitor, TSTypeFile)"/> method.
    /// </para>
    /// <para>
    /// Note that nothing forbids more than one ITSCodeGeneratorType to exist on a type: if it happens, it is up
    /// to the multiple implementations to synchronize their works on the final file along with any global <see cref="ITSCodeGenerator"/> work.
    /// </para>
    /// </summary>
    public interface ITSCodeGeneratorType : ITSCodeGeneratorAutoDiscovery
    {
        /// <summary>
        /// Configures the <see cref="TypeScriptAttribute"/> that will be used
        /// by <see cref="TypeScriptGenerator.GetTSTypeFile"/> to create the file for the type.
        /// <para>
        /// Note that if a global <see cref="ITSCodeGenerator"/> has preempted the code generation (see <see cref="TSTypeFile.GlobalControl"/>),
        /// then this is not called.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="a">
        /// The attribute to configure. May be an empty one or the existing attribute on the type and may
        /// already be configured by other <see cref="ITSCodeGeneratorType"/>.
        /// </param>
        /// <param name="generatorTypes">All the generators bound to this type (including this one).</param>
        /// <returns>True on success, false on error (errors must be logged).</returns>
        bool ConfigureTypeScriptAttribute( IActivityMonitor monitor, TypeScriptAttribute a, IReadOnlyList<ITSCodeGeneratorType> generatorTypes );

        /// <summary>
        /// Generates the TypeScript code. The <paramref name="file"/> exposes the <see cref="TSTypeFile.TypeScriptGenerator"/>.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="file">The file that must be generated (<see cref="TSTypeFile.EnsureFile"/> may be called).</param>
        /// <returns>True on success, false on error (errors must be logged).</returns>
        bool GenerateCode( IActivityMonitor monitor, TSTypeFile file );

    }
}
