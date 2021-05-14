using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.StObj.TypeScript.Engine
{
    /// <summary>
    /// Raised by <see cref="TSIPocoCodeGenerator.PocoGenerated"/>.
    /// </summary>
    public class PocoGeneratedEventArgs : EventMonitoredArgs
    {
        /// <summary>
        /// Initializes a new <see cref="PocoGeneratedEventArgs"/>.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="tsTypedFile">The generated Poco file.</param>
        /// <param name="pocoClassPart">The code part of the poco class.</param>
        /// <param name="pocoInfo">The poco information.</param>
        public PocoGeneratedEventArgs( IActivityMonitor monitor, TSTypeFile tsTypedFile, ITSKeyedCodePart pocoClassPart, IPocoRootInfo pocoInfo )
            : base( monitor )
        {
            TypeFile = tsTypedFile;
            PocoRootInfo = pocoInfo;
            PocoClassPart = pocoClassPart;
        }

        /// <summary>
        /// Gets the generated file.
        /// There is one <see cref="ITSKeyedCodePart"/> by IPoco interface (their key is the interface's type)
        /// in addition to the <see cref="PocoClassPart"/>.
        /// </summary>
        public TSTypeFile TypeFile { get; }

        /// <summary>
        /// Gets the poco class part.
        /// </summary>
        public ITSKeyedCodePart PocoClassPart { get; }

        /// <summary>
        /// Gets the poco information.
        /// </summary>
        public IPocoRootInfo PocoRootInfo { get; }

        /// <summary>
        /// Gets the generation context.
        /// </summary>
        public TypeScriptContext Context => TypeFile.Context;

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
