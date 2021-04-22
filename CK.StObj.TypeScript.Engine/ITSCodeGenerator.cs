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
        /// This method has 2 responsibilities: it can configure the <see cref="TypeScriptAttribute"/> that will be used
        /// by <see cref="TypeScriptGenerator.GetTSTypeFile"/> to create the file for the given <paramref name="type"/>,
        /// and it also enables a global generator to mess with type bound generators by modifying the <paramref name="generatorTypes"/>
        /// mutable list and/or injecting a <paramref name="finalizer"/> function.
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
        /// <param name="generator">The code generator to use.</param>
        /// <param name="type">The type whose TypeScript code must be generated.</param>
        /// <param name="attr">The attribute to configure. May be an empty one or the existing attribute on the type.</param>
        /// <param name="generatorTypes">Mutable list of the generators bound to this type.</param>
        /// <param name="finalizer">
        /// The current optional finalizer function that will be eventually called (regardless of current generation already done).
        /// This function can be replaced/composed as needed.
        /// </param>
        /// <returns>True on success, false on error (errors must be logged).</returns>
        bool ConfigureTypeScriptAttribute( IActivityMonitor monitor,
                                           TypeScriptGenerator generator,
                                           Type type,
                                           TypeScriptAttribute attr,
                                           IList<ITSCodeGeneratorType> generatorTypes,
                                           ref Func<IActivityMonitor, TSTypeFile, bool>? finalizer );

        /// <summary>
        /// Generates any TypeScript in the provided context.
        /// This is called once and only once before type bound methods <see cref="ITSCodeGeneratorType.GenerateCode"/>
        /// are called.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="generator">The generator to use.</param>
        /// <returns>True on success, false on error (errors must be logged).</returns>
        bool GenerateCode( IActivityMonitor monitor, TypeScriptGenerator generator );

    }
}
