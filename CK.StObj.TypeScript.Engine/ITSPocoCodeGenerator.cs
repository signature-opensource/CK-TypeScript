using CK.Core;
using CK.StObj.TypeScript.Engine;
using CK.TypeScript.CodeGen;
using System;

namespace CK.Setup
{
    /// <summary>
    /// Handles TypeScript generation of any type that appears in the <see cref="IPocoTypeSystem"/>.
    /// <para>
    /// One can extend Poco generation by subscribing to events and adding code to the parts.
    /// </para>
    /// </summary>
    public interface ITSPocoCodeGenerator
    {
        /// <summary>
        /// Gets the TypeScript Poco model.
        /// </summary>
        TSPocoModel PocoModel { get; }

        /// <summary>
        /// Gets whether a <see cref="IPocoType"/> is exchangeable: it is <see cref="IPocoType.IsExchangeable"/>
        /// and if <see cref="TypeScriptContext.JsonNames"/> exists, then <see cref="ExchangeableTypeNameMap.IsExchangeable(IPocoType)"/>
        /// is also true.
        /// </summary>
        /// <param name="type">The poco type.</param>
        /// <returns>True if this poco type must be available in TypeScript.</returns>
        bool IsExchangeable( IPocoType type );

        /// <summary>
        /// Raised when generating code of a <see cref="IAbstractPocoType"/>.
        /// </summary>
        event EventHandler<GeneratingAbstractPocoEventArgs>? AbstractPocoGenerating;

        /// <summary>
        /// Raised when generating code of a named <see cref="IRecordPocoType"/>.
        /// </summary>
        event EventHandler<GeneratingNamedRecordPocoEventArgs>? NamedRecordPocoGenerating;

        /// <summary>
        /// Raised when generating code of a <see cref="IPrimaryPocoType"/>.
        /// </summary>
        event EventHandler<GeneratingPrimaryPocoEventArgs>? PrimaryPocoGenerating;
    }
}
