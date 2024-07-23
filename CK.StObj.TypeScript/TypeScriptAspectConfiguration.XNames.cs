using System.Xml.Linq;

namespace CK.Setup
{
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
        /// The <see cref="TypeScriptBinPathAspectConfiguration.SkipTypeScriptTooling"/> attribute name.
        /// </summary>
        public static readonly XName xSkipTypeScriptTooling = XNamespace.None + "SkipTypeScriptTooling";

        /// <summary>
        /// The <see cref="TypeScriptBinPathAspectConfiguration.EnsureTestSupport"/> attribute name.
        /// </summary>
        public static readonly XName xEnsureTestSupport = XNamespace.None + "EnsureTestSupport";

        /// <summary>
        /// The <see cref="TypeScriptBinPathAspectConfiguration.CKGenBuildMode"/> attribute name.
        /// </summary>
        public static readonly XName xCKGenBuildMode = XNamespace.None + "CKGenBuildMode";

        /// <summary>
        /// The <see cref="TypeScriptBinPathAspectConfiguration.ModuleSystem"/> attribute name.
        /// </summary>
        public static readonly XName xModuleSystem = XNamespace.None + "ModuleSystem";

        /// <summary>
        /// The <see cref="TypeScriptBinPathAspectConfiguration.EnableTSProjectReferences"/> attribute name.
        /// </summary>
        public static readonly XName xEnableTSProjectReferences = XNamespace.None + "EnableTSProjectReferences";

        /// <summary>
        /// The <see cref="TypeScriptBinPathAspectConfiguration.AutomaticTypeScriptVersion"/> attribute name.
        /// </summary>
        public static readonly XName xAutomaticTypeScriptVersion = XNamespace.None + "AutomaticTypeScriptVersion";

        /// <summary>
        /// The <see cref="TypeScriptBinPathAspectConfiguration.TypeFilterName"/> attribute name.
        /// </summary>
        public static readonly XName xTypeFilterName = XNamespace.None + "TypeFilterName";

        /// <summary>
        /// The <see cref="TypeScriptBinPathAspectConfiguration.AutoInstallYarn"/> attribute name.
        /// </summary>
        public static readonly XName xAutoInstallYarn = XNamespace.None + "AutoInstallYarn";

        /// <summary>
        /// The <see cref="TypeScriptBinPathAspectConfiguration.GitIgnoreCKGenFolder"/> attribute name.
        /// </summary>
        public static readonly XName xGitIgnoreCKGenFolder = XNamespace.None + "GitIgnoreCKGenFolder";

        /// <summary>
        /// The <see cref="TypeScriptBinPathAspectConfiguration.AutoInstallVSCodeSupport"/> attribute name.
        /// </summary>
        public static readonly XName xAutoInstallVSCodeSupport = XNamespace.None + "AutoInstallVSCodeSupport";

        /// <summary>
        /// The <see cref="TypeScriptBinPathAspectConfiguration"/> element name.
        /// </summary>
        public static readonly XName xTypeScript = XNamespace.None + "TypeScript";

        /// <summary>
        /// The attribute name of <see cref="TypeScriptBinPathAspectConfiguration.TargetProjectPath"/>.
        /// </summary>
        public static readonly XName xTargetProjectPath = XNamespace.None + "TargetProjectPath";

        /// <summary>
        /// The attribute name of <see cref="TypeScriptTypeConfiguration.TypeName"/>.
        /// </summary>
        public static readonly XName xTypeName = XNamespace.None + "TypeName";

        /// <summary>
        /// The attribute name of <see cref="TypeScriptTypeConfiguration.Folder"/>.
        /// </summary>
        public static readonly XName xFolder = XNamespace.None + "Folder";

        /// <summary>
        /// The attribute name of <see cref="TypeScriptTypeConfiguration.FileName"/>.
        /// </summary>
        public static readonly XName xFileName = XNamespace.None + "FileName";

        /// <summary>
        /// The attribute name of <see cref="TypeScriptTypeConfiguration.SameFileAs"/>.
        /// </summary>
        public static readonly XName xSameFileAs = XNamespace.None + "SameFileAs";

        /// <summary>
        /// The attribute name of <see cref="TypeScriptTypeConfiguration.SameFolderAs"/>.
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

}
