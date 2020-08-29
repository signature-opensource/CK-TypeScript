using CK.Core;
using CK.Setup;
using CK.Text;
using CK.TypeScript.CodeGen;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace CK.StObj.TypeScript.Engine
{
    public class TypeScriptImpl : ITSCodeGeneratorAutoDiscovery
    {
        readonly Type _type;

        public TypeScriptImpl( TypeScriptAttribute a, Type type )
        {
            Attribute = a;
            _type = type;
        }

        public TypeScriptAttribute Attribute { get; }

    }
}
