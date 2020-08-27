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
    public interface ITSCodeGeneratorType
    {
        /// <summary>
        /// Configures the <see cref="TypeScriptAttribute"/> that will be used
        /// by <see cref="TypeScriptGenerator.GetTSTypeFile(Type)"/> to create the file for the type.
        /// </summary>
        /// <param name="a">
        /// The attribute to configure. May be an empty one or the existing attribute on the type and may
        /// already be configured by other <see cref="ITSCodeGeneratorType"/> or <see cref="ITSCodeGenerator"/>.
        /// </param>
        void ConfigureTypeScriptAttribute( TypeScriptAttribute a );

        /// <summary>
        /// Generates the TypeScript code. The <paramref name="file"/> exposes
        /// the <see cref="TSTypeFile.Generator"/> and the generator to the current <see cref="TypeScriptGenerator.BinPath"/>.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="file">The file that must be generated (<see cref="TSTypeFile.EnsureFile"/> may be called).</param>
        /// <returns>True on success, false on error (errors must be logged).</returns>
        bool GenerateCode( IActivityMonitor monitor, TSTypeFile file );

    }
}
