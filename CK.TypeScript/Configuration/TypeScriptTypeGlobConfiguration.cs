using CK.Core;
using CK.TypeScript;
using System.Xml.Linq;

namespace CK.Setup;

/// <summary>
/// A <see cref="TypeNamePattern"/> in the <see cref="TypeScriptBinPathAspectConfiguration.Types"/>: it selects the
/// types of the BinPath that match it. Unlike an explicit type (that must be handled by a TypeScript code generator),
/// a pattern only selects:
/// <list type="bullet">
///     <item>The exchangeable Poco compliant types: they are registered.</item>
///     <item>The <see cref="TypeScriptGroup"/> and <see cref="TypeScriptPackage"/>: they are roots of the package selection.</item>
/// </list>
/// The other types are ignored. An explicit type configuration wins over a pattern and the excluded types
/// (<see cref="TypeScriptBinPathAspectConfiguration.ExcludedTypes"/> and <see cref="TypeScriptBinPathAspectConfiguration.ExcludedTypePatterns"/>)
/// are never selected.
/// </summary>
public sealed class TypeScriptTypeGlobConfiguration
{
    /// <summary>
    /// Initializes a new type pattern configuration.
    /// </summary>
    /// <param name="pattern">The pattern.</param>
    /// <param name="configuration">
    /// Optional configuration of the registered Poco types. Only the <see cref="TypeScriptTypeAttribute.Folder"/> can be
    /// set: the type and file names can only apply to a single type.
    /// </param>
    public TypeScriptTypeGlobConfiguration( TypeNamePattern pattern, TypeScriptTypeAttribute? configuration = null )
    {
        Throw.CheckNotNullArgument( pattern );
        if( configuration != null
            && (configuration.TypeName != null || configuration.FileName != null || configuration.SameFileAs != null || configuration.SameFolderAs != null) )
        {
            Throw.ArgumentException( nameof( configuration ), $"Only the Folder can be configured for the type name pattern '{pattern}'." );
        }
        Pattern = pattern;
        Configuration = configuration;
    }

    /// <summary>
    /// Gets the pattern.
    /// </summary>
    public TypeNamePattern Pattern { get; }

    /// <summary>
    /// Gets the optional configuration of the registered Poco types (only the <see cref="TypeScriptTypeAttribute.Folder"/>).
    /// </summary>
    public TypeScriptTypeAttribute? Configuration { get; }

    internal XElement ToXml()
    {
        return new XElement( EngineConfiguration.xType,
                             Configuration?.ToXmlAttributes(),
                             Pattern.ToString() );
    }
}
