using CK.Core;
using CK.TypeScript;
using CK.TypeScript.Engine;
using CK.TypeScript.CodeGen;

namespace CK.Setup;

/// <summary>
/// Global Type Script code generator.
/// <para>
/// The <see cref="ITSCodeGeneratorType"/> should be used if the TypeScript generation can be driven
/// by an attribute on a Type.
/// </para>
/// <para>
/// A global code generator like this coexists with other global and other type bound <see cref="ITSCodeGeneratorType"/>.
/// It is up to the implementation to handle the collaboration (or to raise an error).
/// </para>
/// </summary>
public interface ITSCodeGenerator
{
    private static readonly ITSCodeGenerator _empty = new EmptyCodeGenerator();

    private sealed class EmptyCodeGenerator : ITSCodeGenerator
    {
        public bool OnResolveObjectKey( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromObjectEventArgs e ) => true;

        public bool OnResolveType( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromTypeEventArgs builder ) => true;

        public bool StartCodeGeneration( IActivityMonitor monitor, TypeScriptContext context ) => true;
    }

    /// <summary>
    /// Gets a no-op, empty code generator.
    /// <para>
    /// <see cref="ITSCodeGeneratorFactory.CreateTypeScriptGenerator(IActivityMonitor, ITypeScriptContextInitializer)"/> can return this
    /// generator if nothing must be done.
    /// </para>
    /// </summary>
    public static ITSCodeGenerator Empty => _empty;

    /// <summary>
    /// This is called once the <see cref="TypeScriptContext"/> is setup, in the order of the <see cref="TypeScriptContext.GlobalGenerators"/>
    /// list.
    /// <para>
    /// This can be used to generate any TypeScript in the provided context and/or to subscribe to exposed events like
    /// <see cref="TypeScriptRoot.BeforeCodeGeneration"/>, <see cref="TypeScriptRoot.AfterCodeGeneration"/>, <see cref="PocoCodeGenerator.PrimaryPocoGenerating"/>, etc
    /// ot to other events exposed by other global generators.
    /// </para>
    /// <para>
    /// Events <see cref="TSTypeManager.TSFromObjectRequired"/> and <see cref="TSTypeManager.TSFromTypeRequired"/> should not be
    /// subscribed to as the <see cref="OnResolveObjectKey(IActivityMonitor, TypeScriptContext, RequireTSFromObjectEventArgs)"/> and
    /// <see cref="OnResolveType(IActivityMonitor, TypeScriptContext, RequireTSFromTypeEventArgs)"/> are directly called by
    /// the <see cref="TypeScriptContext"/>.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="context">The TypeScript context.</param>
    /// <returns>True on success, false on error (errors must be logged).</returns>
    bool StartCodeGeneration( IActivityMonitor monitor, TypeScriptContext context );

    /// <summary>
    /// Can configure the <see cref="RequireTSFromTypeEventArgs"/> if the <see cref="RequireTSFromTypeEventArgs.Type"/>
    /// can be handled by this generator.
    /// <para>
    /// If a <see cref="TypeScriptAttribute"/> decorates the type ot the type has been configured, the builder properties
    /// reflects its configuration.
    /// </para>
    /// <para>
    /// Note that this method may be called after the single call to <see cref="StartCodeGeneration"/> because other generators
    /// can call <see cref="TSTypeManager.ResolveTSType(IActivityMonitor, object)"/>.
    /// In practice this should not be an issue and if it is, it is up to this global code generator to correctly handle
    /// these "after my GenerateCode call" new incoming types.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="context">The TypeScript context.</param>
    /// <param name="builder">The builder with the <see cref="RequireTSFromTypeEventArgs.Type"/> that is handled.</param>
    /// <returns>True on success, false on error (errors must be logged).</returns>
    bool OnResolveType( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromTypeEventArgs builder );

    /// <summary>
    /// Can call <see cref="RequireTSFromObjectEventArgs.SetResolvedType(ITSType)"/> if the <see cref="RequireTSFromObjectEventArgs.KeyType"/>
    /// is handled by this generator.
    /// <para>
    /// Note that this method may be called after the single call to <see cref="StartCodeGeneration"/> because other generators
    /// can call <see cref="TSTypeManager.ResolveTSType(IActivityMonitor, object)"/>.
    /// In practice this should not be an issue and if it is, it is up to this global code generator to correctly handle
    /// these "after my GenerateCode call" new incoming types.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="context">The TypeScript context.</param>
    /// <param name="e">
    /// The event with the <see cref="RequireTSFromObjectEventArgs.KeyType"/> that is handled and
    /// the <see cref="RequireTSFromObjectEventArgs.ResolvedType"/> to set.
    /// </param>
    /// <returns>True on success, false on error (errors must be logged).</returns>
    bool OnResolveObjectKey( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromObjectEventArgs e );

}
