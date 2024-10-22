using CK.Setup;
using CK.TypeScript.CodeGen;

namespace CK.TS.Angular.Engine;

/// <summary>
/// Exposes the angular application extension points.
/// </summary>
public interface IAngularContext
{
    /// <summary>
    /// Gets the 'CK/Angular/CKGenAppModule.ts' file.
    /// </summary>
    ITSFileType CKGenAppModule { get; }

    /// <summary>
    /// The import module part.
    /// </summary>
    ITSCodePart ImportModulePart { get; }

    /// <summary>
    /// Gets the import module part.
    /// </summary>
    ITSCodePart ExportModulePart { get; }

    /// <summary>
    /// Gets the 'static Providers : Provider[] = [...]' part.
    /// </summary>
    ITSCodePart ProviderPart { get; }

    /// <summary>
    /// Get the 'CK/Angular/routes.ts' file 'export default [...]' part.
    /// </summary>
    ITSCodePart RoutesPart { get; }
}

/// <summary>
/// Angular related extensions.
/// </summary>
public static class TypeScriptContextExtensions
{
    /// <summary>
    /// Gets the angular context for this TypeScriptContext.
    /// <para>
    /// The <see cref="IAngularContext"/> is registered by the <see cref="AngularCodeGeneratorImpl"/>.
    /// </para>
    /// </summary>
    /// <param name="context">This context.</param>
    /// <returns>The Angular context to use.</returns>
    public static IAngularContext? GetAngularContext( this TypeScriptContext context )
    {
        return context.Root.Memory.TryGetValue( typeof( IAngularContext ), out var o ) ? (IAngularContext?)o : null;
    }
}

