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
        /// Gets the set of Poco types that are handled by this generator.
        /// </summary>
        IPocoTypeSet TypeScriptSet { get;  }

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
