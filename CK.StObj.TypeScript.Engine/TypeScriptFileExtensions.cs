using System;
using System.Collections.Generic;
using CK.Setup;
using CK.Core;
using CK.StObj.TypeScript.Engine;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Extends type script file.
    /// </summary>
    public static class TypeScriptFileExtensions
    {
        /// <summary>
        /// Gets the <see cref="TypeScriptContext"/> of a type script file.
        /// </summary>
        /// <param name="this">This file.</param>
        /// <returns>The <see cref="TypeScriptContext"/>.</returns>
        public static TypeScriptContext GetContext( this TypeScriptFile<TypeScriptContextRoot> @this ) => @this.Folder.Root.Context;

        /// <summary>
        /// Ensures that an import of one or more types exists in this file.
        /// <see cref="TypeScriptContext.DeclareTSType(IActivityMonitor, Type, bool)"/> is called for each type.
        /// </summary>
        /// <param name="this">This file.</param>
        /// <param name="monitor">
        /// Required monitor since <see cref="TypeScriptContext.DeclareTSType(IActivityMonitor, Type, bool)"/> is called.
        /// </param>
        /// <param name="type">The type to import.</param>
        /// <param name="types">Optional types to import.</param>
        public static void EnsureImport( this TypeScriptFile<TypeScriptContextRoot> @this, IActivityMonitor monitor, Type type, params Type[] types )
        {
            Throw.CheckArgument( @this != null && monitor != null && types != null );
            ImportType( @this, monitor, type );
            foreach( var t in types )
            {
                ImportType( @this, monitor, t );
            }
        }

        /// <summary>
        /// Ensures that TypeScript files for types are imported.
        /// <see cref="TypeScriptContext.DeclareTSType(IActivityMonitor, Type, bool)"/> is called for each type.
        /// </summary>
        /// <param name="this">This file.</param>
        /// <param name="monitor">
        /// Required monitor since <see cref="TypeScriptContext.DeclareTSType(IActivityMonitor, Type, bool)"/> is called.
        /// </param>
        /// <param name="types">Types to import.</param>
        public static void EnsureImport( this TypeScriptFile<TypeScriptContextRoot> @this, IActivityMonitor monitor, IEnumerable<Type> types )
        {
            Throw.CheckArgument( @this != null && monitor != null && types != null );
            foreach( var t in types )
            {
                ImportType( @this, monitor, t );
            }
        }

        static void ImportType( TypeScriptFile<TypeScriptContextRoot> file, IActivityMonitor monitor, Type type )
        {
            Throw.CheckNotNullArgument( type );
            var tsType = file.GetContext().DeclareTSType( monitor, type );
            if( tsType != null ) file.Imports.EnsureImport( tsType.File, tsType.TypeName );
        }

        /// <summary>
        /// Ensures that one or more <see cref="TSTypeFile.Type"/> are imported.
        /// </summary>
        /// <param name="this">This file.</param>
        /// <param name="type">Type to import.</param>
        /// <param name="types">Other types to import.</param>
        public static void EnsureImport( this TypeScriptFile<TypeScriptContextRoot> @this, TSTypeFile type, params TSTypeFile[] types )
        {
            Throw.CheckArgument( @this != null && type != null && types != null );
            @this.Imports.EnsureImport( type.File, type.TypeName );
            foreach( var t in types )
            {
                Throw.CheckNotNullArgument( "A null Type appears in TSTypeFile types params.", t );
                @this.Imports.EnsureImport( t.File, t.TypeName );
            }
        }
    }
}
