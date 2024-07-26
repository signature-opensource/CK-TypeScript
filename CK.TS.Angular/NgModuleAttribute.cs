using CK.StObj.TypeScript;

namespace CK.TS.Angular
{
    public class NgModuleAttribute : TypeScriptPackageAttribute
    {
        public NgModuleAttribute()
            : base( "CK.TS.Angular.Engine.NgModuleAttributeImpl, CK.TS.Angular.Engine" )
        {
        }
    }
}
