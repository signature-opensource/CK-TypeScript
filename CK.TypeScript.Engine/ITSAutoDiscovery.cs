namespace CK.TypeScript.Engine;

/// <summary>
/// Marker interface that enables discovery of <see cref="TypeScriptTypeAttributeImpl"/>, global <see cref="CK.Setup.ITSCodeGenerator"/>
/// and <see cref="CK.Setup.ITSCodeGeneratorType"/> in a single pass.
/// </summary>
/// <remarks>
/// This is publicly exposed since base interfaces of public interfaces are required to be public, but this is
/// for internal use.
/// </remarks>
public interface ITSCodeGeneratorAutoDiscovery
{
}
