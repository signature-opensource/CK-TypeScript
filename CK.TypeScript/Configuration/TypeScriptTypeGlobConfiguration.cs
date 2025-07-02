using CK.Core;
using CK.TypeScript;
using System;
using System.Xml.Linq;

namespace CK.Setup;

/// <summary>
/// Types with * wildcard. The * can appear for the type name (this applies to the namespace) and/or the assembly name
/// or as a suffix for them. Leading and internal * are forbidden.
/// <para>
/// When no * appear in the <paramref name="AssemblyQualifiedNamePattern"/>:
/// <list type="bullet">
///     <item>It acts as a <see cref="TypeScriptBinPathAspectConfiguration.ExcludedTypes"/> if <paramref name="RegistrationMode"/> is <see cref="RegistrationMode.Excluded"/>.</item>
///     <item>It is ignored if <paramref name="RegistrationMode"/> is <see cref="RegistrationMode.None"/>.</item>
///     <item>Otherwise, it acts as a <see cref="TypeScriptTypeConfiguration2"/> for matching types.</item>
/// </list>
/// </para>
/// </summary>
/// <param name="AssemblyQualifiedNamePattern">The assembly qualified name pattern to consider.</param>
/// <param name="Configuration">
/// The configuration to apply to the type. When specified, this overrides the <see cref="TypeScriptTypeAttribute2"/> that may
/// decorate the type.
/// </param>
/// <param name="RegistrationMode">The registration mode to consider for all the types.</param>
public sealed record class TypeScriptTypeGlobConfiguration( string AssemblyQualifiedNamePattern,
                                                            TypeScriptTypeAttribute2? Configuration = null,
                                                            RegistrationMode RegistrationMode = RegistrationMode.Regular )
{
    internal XElement? ToXml()
    {
        return RegistrationMode == RegistrationMode.None
            ? null
            : new XElement( EngineConfiguration.xType,
                                RegistrationMode != RegistrationMode.Regular
                                    ? new XAttribute( TypeScriptAspectConfiguration.xRegistrationMode, RegistrationMode.ToString() )
                                    : null,
                                Configuration?.ToXmlAttributes(),
                                AssemblyQualifiedNamePattern );
    }
}
