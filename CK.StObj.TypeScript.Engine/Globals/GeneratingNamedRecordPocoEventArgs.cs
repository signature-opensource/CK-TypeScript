using CK.Core;
using CK.Setup;
using CK.StObj.TypeScript.Engine;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;

namespace CK.Setup
{
    /// <summary>
    /// Raised by <see cref="PocoCodeGenerator.NamedRecordPocoGenerating"/>.
    /// This event enable participants to alter the TypeScript Poco code.
    /// </summary>
    public sealed class GeneratingNamedRecordPocoEventArgs : EventMonitoredArgs
    {
        readonly TypeScriptContext _typeScriptContext;
        readonly ITSFileCSharpType _tsType;
        readonly IRecordPocoType _pocoType;
        readonly ImmutableArray<TSNamedCompositeField> _fields;
        readonly ITSCodePart _ctorParametersPart;
        readonly ITSCodePart _ctorBodyPart;
        Type? _docType;
        Action<DocumentationBuilder>? _documentationExtension;

        internal GeneratingNamedRecordPocoEventArgs( IActivityMonitor monitor,
                                                     TypeScriptContext typeScriptContext,
                                                     ITSFileCSharpType tSGeneratedType,
                                                     IRecordPocoType pocoType,
                                                     ImmutableArray<TSNamedCompositeField> fields,
                                                     ITSCodePart ctorParametersPart,
                                                     ITSCodePart ctorBodyPart )
            : base( monitor )
        {
            _typeScriptContext = typeScriptContext;
            _tsType = tSGeneratedType;
            _pocoType = pocoType;
            _docType = pocoType.Type;
            _fields = fields;
            _ctorParametersPart = ctorParametersPart;
            _ctorBodyPart = ctorBodyPart;
        }

        /// <summary>
        /// Gets the context.
        /// </summary>
        public TypeScriptContext TypeScriptContext => _typeScriptContext;

        /// <summary>
        /// Gets the record poco type that is being generated.
        /// </summary>
        public IRecordPocoType RecordPocoType => _pocoType;

        /// <summary>
        /// Gets the generated TypeScript type.
        /// </summary>
        public ITSFileCSharpType TSGeneratedType => _tsType;

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
        public ImmutableArray<TSNamedCompositeField> Fields => _fields;

        /// <summary>
        /// Gets the part to extend the poco class.
        /// </summary>
        public ITSCodePart PocoTypePart => _tsType.TypePart;
    }
}
