using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;

namespace CK.Setup
{
    /// <summary>
    /// Raised by <see cref="TypeScriptContext.AbstractPocoGenerating"/>.
    /// This event enable participants to alter the TypeScript Poco code.
    /// </summary>
    public sealed class GeneratingAbstractPocoEventArgs : EventMonitoredArgs
    {
        readonly ITSGeneratedType _tsType;
        readonly IAbstractPocoType _pocoType;
        readonly ITSCodePart _interfacesPart;
        readonly ITSCodePart _bodyPart;
        Type? _docType;
        IEnumerable<IAbstractPocoType> _implementedInterfaces;
        Action<DocumentationBuilder>? _documentationExtension;

        internal GeneratingAbstractPocoEventArgs( IActivityMonitor monitor,
                                                  ITSGeneratedType tSGeneratedType,
                                                  IAbstractPocoType pocoType,
                                                  IEnumerable<IAbstractPocoType> implementedInterfaces,
                                                  ITSCodePart interfacesPart,
                                                  ITSCodePart bodyPart )
            : base( monitor )
        {
            _tsType = tSGeneratedType;
            _pocoType = pocoType;
            _docType = pocoType.Type;
            _implementedInterfaces = implementedInterfaces;
            _interfacesPart = interfacesPart;
            _bodyPart = bodyPart;
        }

        /// <summary>
        /// Gets the primary poco type that is being generated.
        /// </summary>
        public IAbstractPocoType AbstractPocoType => _pocoType;

        /// <summary>
        /// Gets the generated TypeScript type.
        /// </summary>
        public ITSGeneratedType TSGeneratedType => _tsType;

        /// <summary>
        /// Gets or sets the interface type from which documentation will be extracted.
        /// <para>
        /// This can be set to null to skip C# documentation adaptation. 
        /// </para>
        /// </summary>
        public Type? TypeDocumentation
        {
            get => _docType;
            set => _docType = value;
        }

        /// <summary>
        /// Gets or sets a documentation writer that can be used to append documentation
        /// after <see cref="TypeDocumentation"/> is written. This is called even if TypeDocumentation
        /// is set to null.
        /// </summary>
        public Action<DocumentationBuilder>? DocumentationExtension
        {
            get => _documentationExtension;
            set => _documentationExtension = value;
        }

        /// <summary>
        /// Gets or sets the base types that will be implemented.
        /// <para>
        /// This can set to substitute a filtered or expanded set if needed. 
        /// </para>
        /// </summary>
        public IEnumerable<IAbstractPocoType> ImplementedInterfaces
        {
            get => _implementedInterfaces;
            set
            {
                Throw.CheckNotNullArgument( value );
                _implementedInterfaces = value;
            }
        }

        /// <summary>
        /// Gets the base interfaces part. By default, <see cref="ImplementedInterfaces"/> will be written in it.
        /// </summary>
        public ITSCodePart InterfacesPart => _interfacesPart;

        /// <summary>
        /// Gets the type body. By default, nothing is written in this part.
        /// </summary>
        public ITSCodePart BodyPart => _bodyPart;
    }
}
