using CK.Setup;
using CK.StObj.TypeScript;
using CK.StObj.TypeScript.Engine;
using CK.Text;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public class TypeScriptGenerator
    {
        readonly Dictionary<Type, TSTypeFile> _typeMappings;

        public TypeScriptGenerator( TypeScriptCodeGenerationContext context, IGeneratedBinPath binPath )
        {
            Context = context;
            BinPath = binPath;
            _typeMappings = new Dictionary<Type, TSTypeFile>();
        }

        /// <summary>
        /// Gets the TypeScript code generation context.
        /// </summary>
        public TypeScriptCodeGenerationContext Context { get; }

        /// <summary>
        /// Gets the current <see cref="IGeneratedBinPath"/> that is being processed.
        /// </summary>
        public IGeneratedBinPath BinPath { get; }

        /// <summary>
        /// Gets the <see cref="TSTypeFile"/> for a type.
        /// </summary>
        /// <param name="t">The type.</param>
        /// <returns>The type file (<see cref="TSTypeFile.File"/> may not yet exists).</returns>
        public TSTypeFile GetTSTypeFile( Type t )
        {
            HashSet<Type>? _ = null;
            return DoGetTSTypeFile( t, ref _ );
        }

        TSTypeFile DoGetTSTypeFile( Type t, ref HashSet<Type>? cycleDetector )
        {
            if( !_typeMappings.TryGetValue( t, out var entry ) )
            {
                var cache = _allTypesAttributesCache.GetValueOrDefault( t );
                TypeScriptAttribute? attr = cache?.GetTypeCustomAttributes<TypeScriptImpl>().FirstOrDefault()?.Attribute;
                ICodeGeneratorTypeScript[]? generators = cache?.GetTypeCustomAttributes<ICodeGeneratorTypeScript>().ToArray();
                if( attr == null && generators != null && generators.Length > 0 )
                {
                    attr = new TypeScriptAttribute();
                    foreach( var g in generators ) g.ConfigureTypeScriptAttribute( attr );
                }
                NormalizedPath folder;
                if( attr?.SameFolderAs != null )
                {
                    if( cycleDetector == null ) cycleDetector = new HashSet<Type>();
                    if( !cycleDetector.Add( t ) ) throw new InvalidOperationException( $"TypeScript.SameFoldeAs cycle detected: {cycleDetector.Select( c => c.Name ).Concatenate( " => " )}." );
                    var f = DoGetTSTypeFile( attr.SameFolderAs, ref cycleDetector );
                    folder = f.Folder;
                }
                else
                {
                    folder = attr?.Folder ?? t.Namespace!.Replace( '.', '/' );
                }
                var defName = t.GetExternalName() ?? t.Name;
                string fileName = attr?.FileName ?? (defName + ".ts");
                string typeName = attr?.TypeName ?? defName;
                entry = new TSTypeFile( this, folder, fileName, typeName );
                _typeMappings.Add( t, entry );
            }
            return entry;
        }

    }
}
