using System;

namespace CK.Setup;

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
    IPocoTypeSet TypeScriptSet { get; }

    /// <summary>
    /// Gets the CTSType file that contains the static CTSType and the SymCTS symbol types
    /// if the serialization is available.
    /// </summary>
    CTSTypeSystem? CTSTypeSystem { get; }

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
