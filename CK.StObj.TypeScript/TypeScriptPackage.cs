using CK.Core;
using CK.Setup;

namespace CK.StObj.TypeScript
{
    /// <summary>
    /// Base class for typescript packages.
    /// </summary>
    [RealObject( ItemKind = DependentItemKindSpec.Container )]
    [StObjProperty( PropertyName = "ResourceLocation", PropertyType = typeof( IResourceLocator ) )]
    [CKTypeDefiner]
    public class TypeScriptPackage : IRealObject
    {
    }

}
