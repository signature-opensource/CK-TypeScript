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
    /// or by a call to <see cref="TypeScriptContext.GetTSTypeFile(IActivityMonitor, Type)"/> or <see cref="TypeScriptContext.DeclareTSType(IActivityMonitor, IEnumerable{Type})"/>
    /// (typically from a global <see cref="ITSCodeGenerator"/>).
    /// Once created, there must be a way to generate the corresponding code into the <see cref="File"/>: at least one participant
    /// must call <see cref="EnsureFile()"/> otherwise an error is raised.
    /// </summary>
    public class TSTypeFile : ITSTypeFileBuilder
    {
        string _toString;
        TypeScriptFile? _file;

        /// <summary>
        /// Discovery constructor. Also memorizes the attribute if it exists (or a new one).
        /// Actual initialization is deferred (this is to handle a single pass on attributes).
        /// Deferred initialization is required because of SameFileAs and SameFolderAs properties.
        /// </summary>
        internal TSTypeFile( TypeScriptContext g, Type t, IList<ITSCodeGeneratorType> generators, TypeScriptAttribute? attr )
        {
            Context = g;
            Type = t;
            Generators = generators;
            Attribute = attr ?? new TypeScriptAttribute();
            _toString = $"TypeScript for '{Type}'";
        }

        internal TypeScriptAttribute Attribute;

        internal void Initialize( NormalizedPath folder, string fileName, string typeName )
        {
            Debug.Assert( !IsInitialized );

            Folder = folder;
            FileName = fileName;
            FullFilePath = folder.AppendPart( fileName );
            TypeName = typeName;
            _toString += $" will be generated in '{FullFilePath}'.";
            _file = Context.Root.Root.FindOrCreateFile( FullFilePath );
        }

        internal bool IsInitialized => FileName != null;

        public override string ToString() => _toString;

        /// <summary>
        /// Gets the <see cref="TypeScriptContext"/>.
        /// </summary>
        public TypeScriptContext Context { get; }

        /// <inheritdoc />
        public Type Type { get; }

        /// <inheritdoc />
        public IList<ITSCodeGeneratorType> Generators { get; }

        /// <inheritdoc />
        public Func<IActivityMonitor, TSTypeFile, bool>? Finalizer { get; set; }

        /// <inheritdoc />
        public void AddFinalizer( Func<IActivityMonitor, TSTypeFile, bool> newFinalizer, bool prepend = false )
        {
            if( newFinalizer != null )
            {
                if( Finalizer != null )
                {
                    var captured = Finalizer;
                    Finalizer = ( m, f ) => prepend
                                                ? newFinalizer( m, f ) && captured( m, f )
                                                : captured( m, f ) && newFinalizer( m, f );
                }
                else
                {
                    Finalizer = newFinalizer;
                }
            }
        }

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
        /// <param name="top">
        /// Optionally creates the new part at the start of the code instead of at the
        /// current writing position in the code.
        /// </param>
        /// <returns>The part for this type.</returns>
        public ITSNamedCodePart EnsureTypePart( string closer = "}", bool top = false ) => File.Body.FindOrCreateNamedPart( TypeName, closer, top );

        internal bool Implement( IActivityMonitor monitor )
        {
            if( Generators.Count > 0 || Finalizer != null )
            {
                foreach( var g in Generators )
                {
                    if( !g.GenerateCode( monitor, this ) ) return false;
                }
                if( Finalizer != null && !Finalizer( monitor, this ) ) return false;
                return true;
            }
            if( TypePart == null )
            {
                // If there is no TypePart, handles the default: only enums are supported.
                if( Type.IsEnum )
                {
                    EnsureTypePart( closer: "" )
                        .AppendEnumDefinition( monitor, Type, TypeName, export: true );
                    return true;
                }
                monitor.Error( $"Missing TypeScript generation in '{FileName}'. Part '{TypeName}' has not been created by any type bound ITSCodeGeneratorType, finalizer generator or global ITSCodeGenerator." );
            }
            return false;
        }
    }
}
