using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Text;

namespace CK.StObj.TypeScript.Engine
{

    /// <summary>
    /// Raised by <see cref="PocoCodeGenerator.PocoGenerating"/>.
    /// </summary>
    public class PocoGeneratingEventArgs : EventMonitoredArgs
    {
        readonly PocoCodeGenerator _pocoCodeGenerator;

        /// <summary>
        /// Initializes a new <see cref="PocoGeneratingEventArgs"/>.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="tsTypedFile">The generated Poco file.</param>
        /// <param name="pocoClass">The poco information.</param>
        /// <param name="pocoCodeGenerator">The code generator.</param>
        public PocoGeneratingEventArgs( IActivityMonitor monitor,
                                        TSTypeFile tsTypedFile,
                                        TypeScriptPocoClass pocoClass,
                                        PocoCodeGenerator pocoCodeGenerator )
            : base( monitor )
        {
            TypeFile = tsTypedFile;
            PocoClass = pocoClass;
            _pocoCodeGenerator = pocoCodeGenerator;
        }

        /// <summary>
        /// Gets the generated file.
        /// There is one <see cref="ITSKeyedCodePart"/> by IPoco interface (their key is the interface's type)
        /// in addition to the <see cref="PocoClass"/>'s part.
        /// </summary>
        public TSTypeFile TypeFile { get; }

        /// <summary>
        /// Gets the poco class description.
        /// </summary>
        public TypeScriptPocoClass PocoClass { get; }

        /// <summary>
        /// Attempts to generate the TypeScript of a IPoco.
        /// Note that reentrant calls are safe: they return the <see cref="TSTypeFile"/> under construction.
        /// </summary>
        /// <remarks>
        /// This enables dependencies among Pocos to be expressed.
        /// </remarks>
        /// <param name="root">The poco for which TypeScript must be generated.</param>
        /// <returns>The type file on success, null on error.</returns>
        public TSTypeFile? EnsurePoco( IPocoRootInfo root ) => _pocoCodeGenerator.EnsurePocoClass( Monitor, TypeFile.Context, root );

        /// <summary>
        /// Gets whether <see cref="SetError(string?)"/> has been called at least once.
        /// </summary>
        public bool HasError { get; private set; }

        /// <summary>
        /// Sets an error.
        /// </summary>
        /// <param name="message">Optional error message to write into the <see cref="EventMonitoredArgs.Monitor">Monitor</see></param>
        public void SetError( string? message = null )
        {
            if( !String.IsNullOrEmpty( message ) )
            {
                Monitor.Error( message );
            }
            HasError = true;
        }

    }
}
