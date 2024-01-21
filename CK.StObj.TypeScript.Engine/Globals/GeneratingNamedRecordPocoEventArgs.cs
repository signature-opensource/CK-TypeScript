using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace CK.Setup
{
    /// <summary>
    /// Raised by <see cref="TypeScriptContext.RaiseGeneratingNamedRecord"/>.
    /// This event enable participants to alter the TypeScript Poco code.
    /// </summary>
    public sealed class GeneratingNamedRecordPocoEventArgs : EventMonitoredArgs
    {
        readonly ITSGeneratedType _tsType;
        readonly IRecordPocoType _pocoType;
        readonly IReadOnlyList<TSPocoField> _fields;
        readonly ITSCodePart _pocoTypeModelPart;
        readonly ITSCodePart _ctorParametersPart;
        readonly ITSCodePart _ctorBodyPart;
        readonly ITSCodePart _bodyPart;
        Type? _docType;
        Action<DocumentationBuilder>? _documentationExtension;

        internal GeneratingNamedRecordPocoEventArgs( IActivityMonitor monitor,
                                                 ITSGeneratedType tSGeneratedType,
                                                 IRecordPocoType pocoType,
                                                 IReadOnlyList<TSPocoField> fields,
                                                 ITSCodePart pocoTypeModelPart,
                                                 ITSCodePart ctorParametersPart,
                                                 ITSCodePart ctorBodyPart,
                                                 ITSCodePart bodyPart )
            : base( monitor )
        {
            _tsType = tSGeneratedType;
            _pocoType = pocoType;
            _docType = pocoType.Type;
            _fields = fields;
            _pocoTypeModelPart = pocoTypeModelPart;
            _ctorParametersPart = ctorParametersPart;
            _ctorBodyPart = ctorBodyPart;
            _bodyPart = bodyPart;
        }

        /// <summary>
        /// Gets the record poco type that is being generated.
        /// </summary>
        public IRecordPocoType RecordPocoType => _pocoType;

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
        public Type? ClassDocumentation
        {
            get => _docType;
            set => _docType = value;
        }

        /// <summary>
        /// Gets or sets a documentation writer that can be used to append documentation
        /// after <see cref="ClassDocumentation"/> is written.
        /// </summary>
        public Action<DocumentationBuilder>? DocumentationExtension
        {
            get => _documentationExtension;
            set => _documentationExtension = value;
        }

        /// <summary>
        /// Gets the constructor parameters part.
        /// </summary>
        public ITSCodePart CtorParametersPart => _ctorParametersPart;

        /// <summary>
        /// Gets the constructor body part. By default, nothing is written in this part.
        /// </summary>
        public ITSCodePart CtorBodyPart => _ctorBodyPart;

        /// <summary>
        /// Gets the type body (after the constructor). By default, nothing is written in this part.
        /// </summary>
        public ITSCodePart BodyPart => _bodyPart;

        /// <summary>
        /// Gets the list of fields that will be written as constructor parameters.
        /// <para>
        /// Fields are ordered in 4 groups:
        /// <list type="number">
        ///   <item>Non nullable, no default (the required).</item>
        ///   <item>Non Nullable with default.</item>
        ///   <item>Nullable with non null default.</item>
        ///   <item>Nullable without default (the optionals).</item>
        /// </list>
        /// </para>
        /// </summary>
        public IReadOnlyList<TSPocoField> Fields => _fields;

        /// <summary>
        /// Gets the part to extend the poco type model.
        /// </summary>
        public ITSCodePart PocoTypeModelPart => _pocoTypeModelPart;
    }
}
