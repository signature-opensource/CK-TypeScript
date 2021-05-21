using CK.Core;
using CK.TypeScript.CodeGen;
using System;
using System.Text;

namespace CK.StObj.TypeScript.Engine
{

    /// <summary>
    /// Raised by <see cref="TSIPocoCodeGenerator.PocoGenerating"/>.
    /// </summary>
    public class PocoGeneratingEventArgs : EventMonitoredArgs
    {
        /// <summary>
        /// Initializes a new <see cref="PocoGeneratingEventArgs"/>.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="tsTypedFile">The generated Poco file.</param>
        /// <param name="pocoClassPart">The code part of the poco class.</param>
        /// <param name="pocoInfo">The poco information.</param>
        public PocoGeneratingEventArgs( IActivityMonitor monitor,
                                        TSTypeFile tsTypedFile,
                                        TypeScriptPocoClass pocoClass )
            : base( monitor )
        {
            TypeFile = tsTypedFile;
            PocoClass = pocoClass;
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
