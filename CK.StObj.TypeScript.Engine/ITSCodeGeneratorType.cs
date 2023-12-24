using CK.Core;
using CK.StObj.TypeScript;
using CK.StObj.TypeScript.Engine;
using CK.TypeScript.CodeGen;

namespace CK.Setup
{
    /// <summary>
    /// Type Script code generator for a type. This interface is typically implemented by delegated attribute classes
    /// (that could specialize <see cref="TypeScriptAttributeImpl"/>).
    /// <para>
    /// Note that nothing forbids more than one ITSCodeGeneratorType to exist on a type: if it happens, it is up
    /// to the multiple implementations to synchronize their works on the final file along with any global <see cref="ITSCodeGenerator"/>
    /// work.
    /// </para>
    /// </summary>
    public interface ITSCodeGeneratorType : ITSCodeGeneratorAutoDiscovery
    {
        /// <summary>
        /// Configures the <see cref="TSGeneratedTypeBuilder"/>.
        /// If a <see cref="TypeScriptAttribute"/> decorates the type, its properties have been applied to the builder.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="context">The global TypeScript context.</param>
        /// <param name="builder">The current builder configuration to configure.</param>
        /// <returns>True on success, false on error (errors must be logged).</returns>
        bool ConfigureBuilder( IActivityMonitor monitor, TypeScriptContext context, TSGeneratedTypeBuilder builder );

        /// <summary>
        /// Generates TypeScript code. The <paramref name="tsType"/> gives access to all the context.
        /// When <see cref="TSGeneratedTypeBuilder.Implementor"/> has been used by <see cref="ConfigureBuilder(IActivityMonitor, TypeScriptContext, TSGeneratedTypeBuilder)"/>
        /// this can perfectly be a no-op and simply return true.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="context">The global TypeScript context.</param>
        /// <param name="tsType">The file that must be generated (<see cref="ITSGeneratedType.EnsureTypePart(string, bool)"/> may be called).</param>
        /// <returns>True on success, false on error (errors must be logged).</returns>
        bool GenerateCode( IActivityMonitor monitor, TypeScriptContext context, ITSGeneratedType tsType );

    }
}
