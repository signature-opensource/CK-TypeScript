using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core;


static class CKCORETypeExtensions
{
    public static string GetWeakTypeName( this Type type )
    {
        return CleanName( type );

        // This is not perfect, it calls type.Assembly.GetName().Name repeatedly.
        // But this works for dynamic type because it doesn't rely on the AssemblyQualifiedName.
        // The ultimate solution would be to use this code for dynamic types and
        // a parser/builder of AssemblyQualifiedName for regular ones.
        // Note that this method is also implemented in CK.Engine.Configuration/EngineConfiguration.Xml.cs
        // 
        static string CleanName( Type type )
        {
            if( type.IsGenericType ) return GetShortGenericName( type, false );
            return $"{type.FullName}, {type.Assembly.GetName().Name}";

            static string GetShortTypeName( Type type, bool inBrackets )
            {
                if( type.IsGenericType ) return GetShortGenericName( type, inBrackets );
                if( inBrackets ) return $"[{type.FullName}, {type.Assembly.GetName().Name}]";
                return $"{type.FullName}, {type.Assembly.GetName().Name}";
            }

            static string GetShortGenericName( Type type, bool inBrackets )
            {
                string? name = type.Assembly.GetName().Name;
                if( inBrackets )
                    return $"[{type.GetGenericTypeDefinition().FullName}[{string.Join( ", ", type.GenericTypeArguments.Select( a => GetShortTypeName( a, true ) ) )}], {name}]";
                else
                    return $"{type.GetGenericTypeDefinition().FullName}[{string.Join( ", ", type.GenericTypeArguments.Select( a => GetShortTypeName( a, true ) ) )}], {name}";
            }
        }
    }
}
