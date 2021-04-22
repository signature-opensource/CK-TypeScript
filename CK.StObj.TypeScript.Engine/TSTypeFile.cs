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
    /// Instances are automatically created because the type has a [<see cref="TypeScriptAttribute"/>] (or other attribute that are implemented by a <see cref="ITSCodeGeneratorType"/>)
    /// or by a call to <see cref="TypeScriptGenerator.GetTSTypeFile(IActivityMonitor, Type)"/> or <see cref="TypeScriptGenerator.DeclareTSType(IActivityMonitor, IEnumerable{Type})"/>
    /// (typically from a global <see cref="ITSCodeGenerator"/>).
    /// Once created, there must be a way to generate the corresponding code into the <see cref="File"/>: at least one participant
    /// must call <see cref="EnsureFile()"/> otherwise an error is raised.
    /// </summary>
    public class TSTypeFile
    {
        string _toString;
        TypeScriptFile? _file;
        Func<IActivityMonitor, TSTypeFile, bool>? _finalizer;

        /// <summary>
        /// Discovery constructor. Also memorizes the attribute if it exists (or a new one).
        /// Actual initialization is deferred (this is to handle a single pass on attributes).
        /// Deferred initialization is required because of SameFileAs and SameFolderAs properties.
        /// </summary>
        internal TSTypeFile( TypeScriptGenerator g, Type t, IList<ITSCodeGeneratorType> generators, TypeScriptAttribute? attr )
        {
            TypeScriptGenerator = g;
            Type = t;
            Generators = generators;
            Attribute = attr ?? new TypeScriptAttribute();
            _toString = $"TypeScript for '{Type}'"; 
        }

        internal TypeScriptAttribute Attribute;

        internal void Initialize( NormalizedPath folder, string fileName, string typeName, Func<IActivityMonitor, TSTypeFile, bool>? finalizer )
        {
            Debug.Assert( !IsInitialized );

            Folder = folder;
            FileName = fileName;
            FullFilePath = folder.AppendPart( fileName );
            TypeName = typeName;
            _finalizer = finalizer;
            _toString += $" will be generated in '{FullFilePath}'.";
            _file = TypeScriptGenerator.Context.Root.FindOrCreateFile( FullFilePath );
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
        /// Gets the associated file.
        /// </summary>
        public TypeScriptFile File
        {
            get
            {
                Debug.Assert( _file != null, "Exposed TSTypedFile has necessarily been initialized." );
                return _file;
            }
        }

        /// <summary>
        /// Gets the code part with the <see cref="TypeName"/> name if it has been created, null otherwise.
        /// The code part may have been created by <see cref="EnsureTypePart(string)"/> or directly on
        /// the file's body. 
        /// </summary>
        public ITSNamedCodePart? TypePart => File.Body.FindNamedPart( TypeName );

        /// <summary>
        /// Ensures that a code part named <see cref="TypeName"/> exists in this file's body.
        /// </summary>
        /// <param name="closer">
        /// By default, the type part will be closed with the "}": a closing
        /// bracket should not be generated and, more importantly, it means that the type part can
        /// easily be extended.
        /// </param>
        /// <returns></returns>
        public ITSNamedCodePart EnsureTypePart( string closer = "}" ) => File.Body.FindOrCreateNamedPart( TypeName, closer );

        /// <summary>
        /// Gets a mutable list of the generators bound to this <see cref="Type"/>.
        /// </summary>
        public IList<ITSCodeGeneratorType> Generators { get; }

        internal bool Implement( IActivityMonitor monitor )
        {
            if( Generators.Count > 0 || _finalizer != null )
            {
                foreach( var g in Generators )
                {
                    if( !g.GenerateCode( monitor, this ) ) return false;
                }
                if( _finalizer != null && !_finalizer( monitor, this ) ) return false;
                return true;
            }
            if( TypePart == null )
            {
                // If there is no TypePart, handles the default: only enums are supported.
                if( Type.IsEnum )
                {
                    EnsureTypePart( closer: "" ).Append( "export " ).AppendEnumDefinition( monitor, Type, TypeName );
                    return true;
                }
                monitor.Error( $"Missing TypeScript generation in '{FileName}'. Part '{TypeName}' has not been created by any type bound ITSCodeGeneratorType, finalizer generator or global ITSCodeGenerator." );
            }
            return false;
        }
    }
}
