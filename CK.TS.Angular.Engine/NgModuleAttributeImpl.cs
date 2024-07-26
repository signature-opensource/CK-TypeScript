using CK.Core;
using CK.Setup;
using CK.StObj.TypeScript;
using CK.StObj.TypeScript.Engine;
using System;

namespace CK.TS.Angular.Engine
{
    public class NgModuleAttributeImpl : TypeScriptPackageAttributeImpl
    {
        public NgModuleAttributeImpl( IActivityMonitor monitor, TypeScriptPackageAttribute attr, Type type, TypeScriptAspect aspect )
            : base( monitor, attr, type, aspect )
        {
        }
    }
}
