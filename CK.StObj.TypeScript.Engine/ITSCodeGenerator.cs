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
    public interface ITSCodeGenerator : ITSCodeGeneratorAutoDiscovery
    {
        /// <summary>
        /// Configures the <see cref="TypeScriptAttribute"/> that will be used by <see cref="TypeScriptContext.DeclareTSType(IActivityMonitor, Type, bool)"/>
        /// to create the Type - File association and allows implementations to freely interact with the <paramref name="builder"/>.
        /// <para>
        /// Note that this method may be called after the single call to <see cref="GenerateCode"/> because of
        /// the <see cref="TypeScriptAttribute.SameFolderAs"/> that may be resolved by the handling of another type
        /// (note that in this case, there is necessarily no TypeScript attribute on the type (<paramref name="attr"/> is a brand new one)
        /// and <paramref name="generatorTypes"/> is empty).
        /// </para>
        /// <para>
        /// In practice this should not be an issue and if is, it up to this global code generator to correctly handle
        /// these "after my GenerateCode call" new incoming types.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="builder">
        /// The builder with the <see cref="ITSTypeFileBuilder.Type"/> that is handled, the <see cref="ITSTypeFileBuilder.Context"/>,
        /// its <see cref="ITSTypeFileBuilder.Generators"/> that includes this one and the current <see cref="ITSTypeFileBuilder.Finalizer"/>.
        /// </param>
        /// <param name="a">
        /// The attribute to configure. May be an empty one or the existing attribute on the type and may
        /// already be configured by previous global <see cref="ITSCodeGenerator"/>.
        /// </param>
        /// <returns>True on success, false on error (errors must be logged).</returns>
        bool ConfigureTypeScriptAttribute( IActivityMonitor monitor,
                                           ITSTypeFileBuilder builder,
                                           TypeScriptAttribute a );

        /// <summary>
        /// Optional extension point (default implementation returns true) called once
        /// all the <see cref="TypeScriptContext.GlobalGenerators"/> have been discovered.
        /// Typically used to subscribe to events that may be raised by other global
        /// generators (like <see cref="TSIPocoCodeGenerator.PocoGenerating"/>).
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="context">The generation context.</param>
        /// <returns>True on success, false on error (errors must be logged).</returns>
        bool Initialize( IActivityMonitor monitor, TypeScriptContext context ) => true;

        /// <summary>
        /// Generates any TypeScript in the provided context.
        /// This is called once and only once before any type bound methods <see cref="ITSCodeGeneratorType.GenerateCode"/>
        /// are called.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="context">The generation context.</param>
        /// <returns>True on success, false on error (errors must be logged).</returns>
        bool GenerateCode( IActivityMonitor monitor, TypeScriptContext context );

    }
}
