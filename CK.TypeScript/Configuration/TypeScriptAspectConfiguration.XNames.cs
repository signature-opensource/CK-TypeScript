using System.Xml.Linq;

namespace CK.Setup;

public sealed partial class TypeScriptAspectConfiguration
{
    /// <summary>
    /// The <see cref="PascalCase"/> attribute name.
    /// </summary>
    public static readonly XName xPascalCase = XNamespace.None + "PascalCase";

    /// <summary>
    /// The <see cref="GenerateDocumentation"/> attribute name.
    /// </summary>
    public static readonly XName xGenerateDocumentation = XNamespace.None + "GenerateDocumentation";

    /// <summary>
    /// The element name of <see cref="TypeScriptAspectConfiguration.LibraryVersions"/>.
    /// </summary>
    public static readonly XName xLibraryVersions = XNamespace.None + "LibraryVersions";

    /// <summary>
    /// The element name of <see cref="TypeScriptAspectConfiguration.LibraryVersions"/> child elements.
    /// </summary>
    public static readonly XName xLibrary = XNamespace.None + "Library";

    /// <summary>
    /// The <see cref="IgnoreVersionsBound"/> attribute name.
    /// </summary>
    public static readonly XName xIgnoreVersionsBound = XNamespace.None + "IgnoreVersionsBound";

    /// <summary>
    /// The <see cref="TypeScriptBinPathAspectConfiguration.IntegrationMode"/> attribute name.
    /// </summary>
    public static readonly XName xIntegrationMode = XNamespace.None + "IntegrationMode";

    /// <summary>
    /// The <see cref="TypeScriptBinPathAspectConfiguration.AutoInstallJest"/> attribute name.
    /// </summary>
    public static readonly XName xAutoInstallJest = XNamespace.None + "AutoInstallJest";

    /// <summary>
    /// The <see cref="TypeScriptBinPathAspectConfiguration.ActiveCultures"/> attribute name.
    /// </summary>
    public static readonly XName xActiveCultures = XNamespace.None + "ActiveCultures";

    /// <summary>
    /// The <see cref="TypeScriptBinPathAspectConfiguration.EnableTSProjectReferences"/> attribute name.
    /// </summary>
    public static readonly XName xEnableTSProjectReferences = XNamespace.None + "EnableTSProjectReferences";

    /// <summary>
    /// The <see cref="TypeScriptBinPathAspectConfiguration.DefaultTypeScriptVersion"/> attribute name.
    /// </summary>
    public static readonly XName xDefaultTypeScriptVersion = XNamespace.None + "DefaultTypeScriptVersion";

    /// <summary>
    /// The <see cref="TypeScriptBinPathAspectConfiguration.TypeFilterName"/> attribute name.
    /// </summary>
    public static readonly XName xTypeFilterName = XNamespace.None + "TypeFilterName";

    /// <summary>
    /// The <see cref="TypeScriptBinPathAspectConfiguration.InstallYarn"/> attribute name.
    /// </summary>
    public static readonly XName xInstallYarn = XNamespace.None + "InstallYarn";

    /// <summary>
    /// The <see cref="TypeScriptBinPathAspectConfiguration.GitIgnoreCKGenFolder"/> attribute name.
    /// </summary>
    public static readonly XName xGitIgnoreCKGenFolder = XNamespace.None + "GitIgnoreCKGenFolder";

    /// <summary>
    /// The <see cref="TypeScriptBinPathAspectConfiguration"/> element name.
    /// </summary>
    public static readonly XName xTypeScript = XNamespace.None + "TypeScript";

    /// <summary>
    /// The attribute name of <see cref="TypeScriptBinPathAspectConfiguration.TargetProjectPath"/>.
    /// </summary>
    public static readonly XName xTargetProjectPath = XNamespace.None + "TargetProjectPath";

    /// <summary>
    /// The attribute name of <see cref="TypeScriptTypeConfiguration2.Required"/>.
    /// </summary>
    public static readonly XName xRequired = XNamespace.None + "Required";

    /// <summary>
    /// The attribute name of <see cref="TypeScriptTypeGlobConfiguration.RegistrationMode"/>.
    /// </summary>
    public static readonly XName xRegistrationMode = XNamespace.None + "RegistrationMode";

    /// <summary>
    /// The attribute name of <see cref="TypeScriptTypeAttribute2.TypeName"/>.
    /// </summary>
    public static readonly XName xTypeName = XNamespace.None + "TypeName";

    /// <summary>
    /// The attribute name of <see cref="TypeScriptTypeAttribute2.Folder"/>.
    /// </summary>
    public static readonly XName xFolder = XNamespace.None + "Folder";

    /// <summary>
    /// The attribute name of <see cref="TypeScriptTypeAttribute2.FileName"/>.
    /// </summary>
    public static readonly XName xFileName = XNamespace.None + "FileName";

    /// <summary>
    /// The attribute name of <see cref="TypeScriptTypeAttribute2.SameFileAs"/>.
    /// </summary>
    public static readonly XName xSameFileAs = XNamespace.None + "SameFileAs";

    /// <summary>
    /// The attribute name of <see cref="TypeScriptTypeAttribute2.SameFolderAs"/>.
    /// </summary>
    public static readonly XName xSameFolderAs = XNamespace.None + "SameFolderAs";

    /// <summary>
    /// The <see cref="TypeScriptBinPathAspectConfiguration.Barrels"/> element name.
    /// </summary>
    public static readonly XName xBarrels = XNamespace.None + "Barrels";

    /// <summary>
    /// The child element name of <see cref="TypeScriptBinPathAspectConfiguration.Barrels"/>.
    /// </summary>
    public static readonly XName xBarrel = XNamespace.None + "Barrel";

    /// <summary>
    /// The <see cref="DeferFileSave"/> attribute name.
    /// </summary>
    public static readonly XName xDeferFileSave = XNamespace.None + "DeferFileSave";

}
