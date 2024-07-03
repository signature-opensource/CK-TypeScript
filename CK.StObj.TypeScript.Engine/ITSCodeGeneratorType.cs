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
        /// Configures the <see cref="RequireTSFromTypeEventArgs"/>.
        /// If a <see cref="TypeScriptAttribute"/> decorates the type, its properties have been applied to the builder.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="context">The global TypeScript context.</param>
        /// <param name="builder">The <see cref="ITSFileCSharpType"/> builder to configure.</param>
        /// <returns>True on success, false on error (errors must be logged).</returns>
        bool ConfigureBuilder( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromTypeEventArgs builder );
    }
}
