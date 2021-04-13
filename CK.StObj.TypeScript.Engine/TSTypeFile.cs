using CK.Core;
using CK.Setup;
using CK.Text;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

namespace CK.StObj.TypeScript.Engine
{

    /// <summary>
    /// Centralizes code generation information for a type.
    /// </summary>
    public class TSTypeFile
    {
        /// <summary>
        /// Discovery constructor. Also memorizes the attribute if it exists (or a new one).
        /// Actual initialization is deferred (this is to handle a single pass on attributes).
        /// Deferred initialization is required because of SameFileAs and SameFolderAs properties.
        /// </summary>
        internal TSTypeFile( TypeScriptGenerator g, Type t, IReadOnlyList<ITSCodeGeneratorType> generators, TypeScriptAttribute? attr )
        {
            TypeScriptGenerator = g;
            Type = t;
            Generators = generators;
            Attribute = attr ?? new TypeScriptAttribute();
        }

        internal TypeScriptAttribute Attribute;

        internal void Initialize( NormalizedPath folder, string fileName, string typeName, ITSCodeGenerator? globalControl )
        {
            Folder = folder;
            FileName = fileName;
            FullFilePath = folder.AppendPart( fileName );
            TypeName = typeName;
            GlobalControl = globalControl;
        }

        internal bool IsInitialized => FileName != null;

        internal TSTypeFile( TypeScriptGenerator g, NormalizedPath folder, string fileName, string typeName, Type t, ITSCodeGenerator? globalControl, IReadOnlyList<ITSCodeGeneratorType> generators )
            : this( g, t, generators, null )
        {
            Initialize( folder, fileName, typeName, globalControl );
        }

        /// <summary>
        /// Gets the <see cref="Setup.TypeScriptGenerator"/>.
        /// </summary>
        public TypeScriptGenerator TypeScriptGenerator { get; }

        /// <summary>
        /// Gets the <see cref="Type"/> for which a TypeScript file must be generated.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Gets the folder that will contain the TypeScript generated code.
        /// </summary>
        public NormalizedPath Folder { get; private set; }

        /// <summary>
        /// Gets the file name.
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// Gets the <see cref="Folder"/>/<see cref="FileName"/> full path.
        /// </summary>
        public NormalizedPath FullFilePath { get; private set; }

        /// <summary>
        /// Gets the TypeScript type name to use.
        /// </summary>
        public string TypeName { get; private set; }

        /// <summary>
        /// Gets the global code generator that take the control of the code generation: none of the
        /// other <see cref="ITSCodeGenerator.GenerateCode"/> nor <see cref="ITSCodeGeneratorType.GenerateCode"/> will be called.
        /// When null, all <see cref="Generators"/> are used and when there is no type generator at all, a default implementation
        /// (only for enums) is used.
        /// </summary>
        public ITSCodeGenerator? GlobalControl { get; private set; }

        /// <summary>
        /// Gets the associated file if <see cref="EnsureFile"/> has been called.
        /// </summary>
        public TypeScriptFile? File { get; private set; }

        /// <summary>
        /// Gets all the generators bound to this <see cref="Type"/>.
        /// </summary>
        public IReadOnlyList<ITSCodeGeneratorType> Generators { get; }

        /// <summary>
        /// Ensures that the <see cref="File"/> has been created.
        /// </summary>
        /// <returns>The associated file.</returns>
        public TypeScriptFile EnsureFile() => File ??= TypeScriptGenerator.Context.Root.FindOrCreateFile( FullFilePath );

        internal bool Implement( IActivityMonitor monitor )
        {
            Debug.Assert( GlobalControl == null );
            if( Generators.Count > 0 )
            {
                foreach( var g in Generators )
                {
                    if( !g.GenerateCode( monitor, this ) ) return false;
                }
                return true;
            }
            // If there is no global or type bound code generator, handles the default: only enums are supported.
            if( Type.IsEnum )
            {
                EnsureFile().Body.Append( "export " ).AppendEnumDefinition( monitor, Type, TypeName );
                return true;
            }
            monitor.Error( $"TypeScript generation for '{Type.Name}' must be handled by a global ITSCodeGenerator or a ITSCodeGeneratorType." );
            return false;
        }
    }
}
