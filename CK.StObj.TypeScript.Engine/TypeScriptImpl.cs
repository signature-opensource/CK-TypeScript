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
    /// <summary>
    /// Internal class that is behind the <see cref="TypeScriptAttribute"/>.
    /// This is not a <see cref="ITSCodeGeneratorType"/>: it just captures the
    /// optional configuration of TypeScript code generation (<see cref="TypeScriptAttribute.FileName"/>,
    /// <see cref="TypeScriptAttribute.Folder"/>, etc.).
    /// </summary>
    internal class TypeScriptImpl : ITSCodeGeneratorAutoDiscovery
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
