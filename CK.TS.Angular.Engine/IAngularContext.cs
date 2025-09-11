using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;

namespace CK.TS.Angular.Engine;

/// <summary>
/// Exposes the angular application extension points.
/// </summary>
public interface IAngularContext
{
    /// <summary>
    /// Gets the 'CK/Angular/CKGenAppModule.ts' import section.
    /// <para>
    /// The file itself is not exposed because it has no reason to be altered
    /// and no type must be created in it (the CKGenAppModule.ts officially exports
    /// no types because it doesn't appear in the root barrel 'index.ts'). 
    /// </para>
    /// </summary>
    ITSFileImportSection CKGenAppModuleImports { get; }

    /// <summary>
    /// Gets the '@angular/core' library.
    /// </summary>
    LibraryImport AngularCoreLibrary { get; }

    /// <summary>
    /// Gets the '@angular/router' library.
    /// </summary>
    LibraryImport AngularRouterLibrary { get; }

    /// <summary>
    /// Gets the 'static Providers : Provider[] = [...]' part.
    /// </summary>
    ITSCodePart ProviderPart { get; }
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
    public static IAngularContext GetAngularContext( this TypeScriptContext context ) => GetAngularCodeGen( context );

    internal static AngularCodeGeneratorImpl.AngularCodeGen GetAngularCodeGen( this TypeScriptContext context )
    {
        if( context.Root.Memory.TryGetValue( typeof( AngularCodeGeneratorImpl.AngularCodeGen ), out var o ) )
        {
            Throw.DebugAssert( o != null );
            return (AngularCodeGeneratorImpl.AngularCodeGen)o;
        }
        throw new InvalidOperationException( "Angular support has not been initialized." );
    }

}

