using CK.Core;
using CK.StObj.TypeScript;
using CK.StObj.TypeScript.Engine;
using CK.TypeScript.CodeGen;
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
        /// Optional extension point called once all the <see cref="TypeScriptContext.GlobalGenerators"/>
        /// have been discovered.
        /// Typically used to subscribe to <see cref="TSTypeManager.BuilderRequired"/>, <see cref="TypeScriptGenerator.BeforeCodeGeneration"/>
        /// or <see cref="TypeScriptGenerator.AfterCodeGeneration"/> events.
        /// This can also be used to subscribe to other events that may be raised by other global
        /// generators (like <see cref="PocoCodeGenerator.PocoGenerating"/>).
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="context">The generation context.</param>
        /// <returns>True on success, false on error (errors must be logged).</returns>
        bool Initialize( IActivityMonitor monitor, TypeScriptContext context );

        /// <summary>
        /// Configures the <see cref="TSGeneratedTypeBuilder"/> (this is called for each <see cref="TSTypeManager.BuilderRequired"/> event).
        /// If a <see cref="TypeScriptAttribute"/> decorates the type, its properties have been applied to the builder.
        /// <para>
        /// Note that this method may be called after the single call to <see cref="GenerateCode"/> because other generators
        /// can call <see cref="TSTypeManager.ResolveTSType(IActivityMonitor, Type)"/>.
        /// In practice this should not be an issue and if it is, it is up to this global code generator to correctly handle
        /// these "after my GenerateCode call" new incoming types.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="context">The global TypeScript context.</param>
        /// <param name="builder">The builder with the <see cref="TSGeneratedTypeBuilder.Type"/> that is handled.</param>
        /// <returns>True on success, false on error (errors must be logged).</returns>
        bool ConfigureBuilder( IActivityMonitor monitor, TypeScriptContext context, TSGeneratedTypeBuilder builder );

        /// <summary>
        /// Generates any TypeScript in the provided context.
        /// This is called once and only once before any type bound methods <see cref="ITSCodeGeneratorType.GenerateCode"/>
        /// are called.
        /// <para>
        /// This method can be a no-op if this generator choose to use the <see cref="ITSTypeFileBuilder.AddFinalizer(Func{IActivityMonitor, TSDecoratedType, bool}, bool)"/>
        /// during <see cref="ConfigureTypeScriptAttribute(IActivityMonitor, ITSTypeFileBuilder, TypeScriptAttribute)"/> for types it can handle. 
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="context">The generation context.</param>
        /// <returns>True on success, false on error (errors must be logged).</returns>
        bool GenerateCode( IActivityMonitor monitor, TypeScriptContext context );

    }
}
