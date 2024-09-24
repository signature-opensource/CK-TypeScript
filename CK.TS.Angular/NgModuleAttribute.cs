using CK.StObj.TypeScript;

namespace CK.TS.Angular;

public class NgModuleAttribute : TypeScriptPackageAttribute
{
    public NgModuleAttribute()
        : base( "CK.TS.Angular.Engine.NgModuleAttributeImpl, CK.TS.Angular.Engine" )
    {
    }

    /// <summary>
    /// Gets or sets whether this module is lazy loaded.
    /// <para>
    /// Defaults to false.
    /// </para>
    /// </summary>
    public bool IsLazyLoaded { get; set; }
}
