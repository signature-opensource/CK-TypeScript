using CK.TypeScript;
using System.Xml.Linq;

namespace CK.Setup;

/// <summary>
/// Types with * wildcard. The * can appear for the type name (this applies to the namespace) and/or the assembly name
/// or as a suffix for them. Leading and internal * are forbidden.
/// </summary>
/// <param name="AssemblyQualifiedNamePattern">The assembly qualified name pattern to consider.</param>
/// <param name="Configuration">
/// The configuration to apply to the type. When specified, this overrides the <see cref="TypeScriptTypeAttribute"/> that may
/// decorate the type.
/// </param>
/// <param name="RegistrationMode">The registration mode to consider for all the types.</param>
public sealed record class TypeScriptTypeGlobConfiguration( string AssemblyQualifiedNamePattern,
                                                            TypeScriptTypeAttribute? Configuration = null )
{
    internal XElement? ToXml()
    {
        return new XElement( EngineConfiguration.xType,
                             Configuration?.ToXmlAttributes(),
                             AssemblyQualifiedNamePattern );
    }
}
