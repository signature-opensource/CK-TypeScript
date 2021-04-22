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
    /// Centralizes code generation information for a <see cref="Type"/>.
    /// Instances are automatically created once by a call to <see cref="TypeScriptGenerator.GetTSTypeFile(IActivityMonitor, Type)"/>
    /// and as soon as it has been created, there must be a way to generate the corresponding code into the <see cref="File"/>:
    /// <list type="bullet">
    ///   <item>The associated <see cref="GlobalControl"/> generator does the job (<see cref="ITSCodeGenerator.GenerateCode(IActivityMonitor, TypeScriptGenerator)"/> handles it).</item>
    ///   <item>If <see cref="GlobalControl"/> is null then the type bound <see cref="Generators"/> are used (by calling <see cref="ITSCodeGeneratorType.GenerateCode(IActivityMonitor, TSTypeFile)"/>).</item>
    ///   <item>If there is no generator at all, then an error is raised (except for <see cref="Enum"/> for which a default generator exists).</item>
    /// </list>
    /// </summary>
    public class TSTypeFile
    {
        string _toString;

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
            _toString = $"TypeScript for '{Type}'"; 
        }

        internal TypeScriptAttribute Attribute;

        internal void Initialize( NormalizedPath folder, string fileName, string typeName, ITSCodeGenerator? globalControl )
        {
            Debug.Assert( !IsInitialized );

            Folder = folder;
            FileName = fileName;
            FullFilePath = folder.AppendPart( fileName );
            TypeName = typeName;
            GlobalControl = globalControl;

            _toString += $" will be generated in '{FullFilePath}' by ";
            if( globalControl != null )
            {
                _toString += $"the '{globalControl.GetType().Name}' global {nameof( ITSCodeGenerator )}."; 
            }
            else
            {
                if( Generators.Count == 0 )
                {
                    _toString += "... no generator. ";
                    if( Type.IsEnum )
                    {
                        _toString += $" But since this is an enum, a default generation will be done.";

                    }
                    else
                    {
                        _toString += $" This should eventually fail.";
                    }
                }
                else
                {
                    _toString += $" {Generators.Count} type bound {nameof( ITSCodeGeneratorType )}.";
                }
            }
        }

        internal bool IsInitialized => FileName != null;

        public override string ToString() => _toString;

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
