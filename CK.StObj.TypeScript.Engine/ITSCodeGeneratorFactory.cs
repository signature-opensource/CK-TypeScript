using CK.Core;
using CK.StObj.TypeScript.Engine;

namespace CK.Setup;

/// <summary>
/// Factory for <see cref="ITSCodeGenerator"/>.
/// <para>
/// This can be used to generate any TypeScript code or to generate code for a set of types without the need
/// of explicit attributes.
/// </para>
/// </summary>
public interface ITSCodeGeneratorFactory : ITSCodeGeneratorAutoDiscovery
{
    /// <summary>
    /// Factory method for <see cref="ITSCodeGenerator"/>.
    /// In addition to create a global code generator, this can call <see cref="ITypeScriptContextInitializer.EnsureRegister"/>
    /// to register C# types that must have an associated TypeScript projection.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="initializer">The TypeScriptContext initializer.</param>
    /// <returns>The generator on success, null on error (errors must be logged).</returns>
    ITSCodeGenerator? CreateTypeScriptGenerator( IActivityMonitor monitor, ITypeScriptContextInitializer initializer );
}
