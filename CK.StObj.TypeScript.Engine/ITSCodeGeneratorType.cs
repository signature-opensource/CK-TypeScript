using CK.Core;
using CK.StObj.TypeScript;
using CK.StObj.TypeScript.Engine;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Type Script code generator for a type. This interface is typically implemented by delegated attribute classes
    /// (that could specialize <see cref="TypeScriptAttributeImpl"/>).
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
        /// Configures the <see cref="TypeScriptAttribute"/> that will be used by <see cref="TypeScriptContext.DeclareTSType(IActivityMonitor, Type, bool)"/>
        /// to create the Type - File association and allows implementations to freely interact with the <paramref name="builder"/>.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="builder">
        /// The builder with the <see cref="ITSTypeFileBuilder.Type"/> that is handled, the <see cref="ITSTypeFileBuilder.Context"/>,
        /// its <see cref="ITSTypeFileBuilder.Generators"/> that includes this one and the current <see cref="ITSTypeFileBuilder.Finalizer"/>.
        /// </param>
        /// <param name="a">
        /// The attribute to configure. May be an empty one or the existing attribute on the type and may
        /// already be configured by other global <see cref="ITSCodeGenerator"/> or by previous <see cref="ITSCodeGeneratorType"/>.
        /// </param>
        /// <returns>True on success, false on error (errors must be logged).</returns>
        bool ConfigureTypeScriptAttribute( IActivityMonitor monitor,
                                           ITSTypeFileBuilder builder,
                                           TypeScriptAttribute a );

        /// <summary>
        /// Generates the TypeScript code. The <paramref name="file"/> exposes the <see cref="TSTypeFile.Context"/>.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="file">The file that must be generated (<see cref="TSTypeFile.EnsureFile"/> may be called).</param>
        /// <returns>True on success, false on error (errors must be logged).</returns>
        bool GenerateCode( IActivityMonitor monitor, TSTypeFile file );

    }
}
