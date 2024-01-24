using CK.Core;
using CK.Setup;
using CK.StObj.TypeScript.Engine;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace CK.Setup
{
    /// <summary>
    /// Raised by <see cref="TypeScriptContext.PrimaryPocoGenerating"/>.
    /// This event enable participants to alter the TypeScript Poco code.
    /// </summary>
    public sealed class GeneratingPrimaryPocoEventArgs : EventMonitoredArgs
    {
        readonly ITSFileCSharpType _tsType;
        readonly IPrimaryPocoType _pocoType;
        readonly IReadOnlyList<TSPocoField> _fields;
        readonly ITSCodePart _pocoTypeModelPart;
        readonly ITSCodePart _interfacesPart;
        readonly ITSCodePart _ctorParametersPart;
        readonly ITSCodePart _ctorBodyPart;
        readonly TSPocoModel _tsPocoModel;
        IEnumerable<Type> _docTypes;
        IEnumerable<IAbstractPocoType> _implementedInterfaces;
        Action<DocumentationBuilder>? _documentationExtension;

        internal GeneratingPrimaryPocoEventArgs( IActivityMonitor monitor,
                                                 TSPocoModel tsPocoModel,
                                                 ITSFileCSharpType tSGeneratedType,
                                                 IPrimaryPocoType pocoType,
                                                 IEnumerable<IAbstractPocoType> implementedInterfaces,
                                                 IReadOnlyList<TSPocoField> fields,
                                                 ITSCodePart pocoTypeModelPart,
                                                 ITSCodePart interfacesPart,
                                                 ITSCodePart ctorParametersPart,
                                                 ITSCodePart ctorBodyPart )
            : base( monitor )
        {
            _tsPocoModel = tsPocoModel;
            _tsType = tSGeneratedType;
            _pocoType = pocoType;
            _docTypes = pocoType.SecondaryTypes.Select( s => s.Type ).Prepend( pocoType.Type );
            _implementedInterfaces = implementedInterfaces;
            _fields = fields;
            _pocoTypeModelPart = pocoTypeModelPart;
            _interfacesPart = interfacesPart;
            _ctorParametersPart = ctorParametersPart;
            _ctorBodyPart = ctorBodyPart;
        }

        /// <summary>
        /// Gets the IPoco model.
        /// </summary>
        public TSPocoModel TSPocoModel => _tsPocoModel;

        /// <summary>
        /// Gets the primary poco type that is being generated.
        /// </summary>
        public IPrimaryPocoType PrimaryPocoType => _pocoType;

        /// <summary>
        /// Gets the generated TypeScript type.
        /// </summary>
        public ITSFileCSharpType TSGeneratedType => _tsType;

        /// <summary>
        /// Gets or sets the types that will be used to generate
        /// the documentation. It starts with the <see cref="PrimaryPocoType"/>'s type
        /// followed by all the <see cref="IPrimaryPocoType.SecondaryTypes"/> types.
        /// <para>
        /// This can set to substitute a filtered or expanded set if needed. 
        /// </para>
        /// </summary>
        public IEnumerable<Type> ClassDocumentation
        {
            get => _docTypes;
            set
            {
                Throw.CheckNotNullArgument( value );
                _docTypes = value;
            }
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
        /// Gets the constructor parameters part.
        /// </summary>
        public ITSCodePart CtorParametersPart => _ctorParametersPart;

        /// <summary>
        /// Gets the constructor body part. By default, nothing is written in this part.
        /// </summary>
        public ITSCodePart CtorBodyPart => _ctorBodyPart;

        /// <summary>
        /// Gets the list of fields that will be written as constructor parameters.
        /// Note that as these are Poco fields, none of them are required, all af them have
        /// a sensible default value.
        /// <para>
        /// Fields are ordered in 3 groups:
        /// <list type="number">
        ///   <item>Non Nullable with default.</item>
        ///   <item>Nullable with non null default.</item>
        ///   <item>Nullable without default (the truly optional ones).</item>
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
