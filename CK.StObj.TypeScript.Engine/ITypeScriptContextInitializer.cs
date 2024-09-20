using CK.Core;
using CK.Setup.PocoJson;
using CK.StObj.TypeScript;
using System;
using System.Collections.Generic;

namespace CK.Setup
{
    /// <summary>
    /// Initialization context provided to global <see cref="ITSCodeGenerator.Initialize(IActivityMonitor, ITypeScriptContextInitializer)"/>.
    /// This enables code generators to register new types (including types that happens to be <see cref="IPocoType"/>) that must be mapped
    /// to TypeScript.
    /// </summary>
    public interface ITypeScriptContextInitializer
    {
        /// <summary>
        /// Gets the set of exchangeable <see cref="IPocoType"/>.
        /// </summary>
        IPocoTypeSet AllExchangeableSet { get; }

        /// <summary>
        /// Gets the discovered global generators.
        /// </summary>
        IReadOnlyList<ITSCodeGenerator> GlobalGenerators { get; }

        /// <summary>
        /// Gets the Json serialization service it Json serialization is available.
        /// </summary>
        IPocoJsonSerializationServiceEngine? JsonSerialization { get; }

        /// <summary>
        /// Gets the Poco type system.
        /// </summary>
        IPocoTypeSystem PocoTypeSystem { get; }

        /// <summary>
        /// Gets the registered types.
        /// </summary>
        IReadOnlyDictionary<Type, RegisteredType> RegisteredTypes { get; }

        /// <summary>
        /// Ensures that a type is registered. If the type is a registered <see cref="IPocoType"/> it
        /// must be in the <see cref="IPocoTypeSetManager.AllExchangeable"/> set to be considered
        /// and added to the <see cref="ITSPocoCodeGenerator.TypeScriptSet"/>.
        /// <list type="bullet">
        ///     <item>If it is not exchangeable and <paramref name="mustBePocoType"/> is true an error is logged and false is return.</item>
        ///     <item>
        ///     If it is not exchangeable and <paramref name="mustBePocoType"/> is false a warning is logged and the type is regsitered
        ///     as a C# type: a <see cref="ITSCodeGenerator"/> must be able to handle its TypeScript code generation.
        ///     </item>
        /// </list>
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        /// <param name="t">The type that must be mapped in TypeScript.</param>
        /// <param name="mustBePocoType">True if the type must be a Poco compliant type.</param>
        /// <param name="attributeConfigurator">Optional factory or updater of the associated <see cref="TypeScriptAttribute"/>.</param>
        /// <returns>
        /// True on success, false on error (when the type is a non exchangeable Poco type and <paramref name="mustBePocoType"/> is true).
        /// </returns>
        bool EnsureRegister( IActivityMonitor monitor,
                             Type t,
                             bool mustBePocoType,
                             Func<TypeScriptAttribute?, TypeScriptAttribute?>? attributeConfigurator = null );

        /// <inheritdoc cref="TypeScriptContext.BinPathConfiguration"/>
        TypeScriptBinPathAspectConfiguration BinPathConfiguration { get; }

        /// <inheritdoc cref="TypeScriptContext.IntegrationContext"/>
        TypeScriptIntegrationContext? IntegrationContext { get; }

    }
}
