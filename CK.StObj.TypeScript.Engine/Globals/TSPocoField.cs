using CK.Core;
using CK.Setup;
using CK.StObj.TypeScript.Engine;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace CK.Setup
{
    /// <summary>
    /// Expose Poco fields that will appear in the constructor, allowing them to be skipped
    /// and/or to alter its documentation.
    /// </summary>
    public sealed class TSPocoField
    {
        readonly PocoCodeGenerator.TSField _f;
        readonly ITSKeyedCodePart _fieldModelPart;
        IEnumerable<XElement> _docs;
        Action<DocumentationBuilder>? _documentationExtension;
        bool _constrctorSkip;

        internal TSPocoField( ITSCodePart parantPart, PocoCodeGenerator.TSField f )
        {
            _f = f;
            _fieldModelPart = parantPart.CreateKeyedPart( this, closer: "}" );
            _docs = _f.Docs;
        }

        /// <summary>
        /// Gets the Poco field.
        /// </summary>
        public IPocoField Field => _f.Field;

        /// <summary>
        /// Gets the TypeScript field type.
        /// </summary>
        public ITSType TSFieldType => _f.TSFieldType;

        /// <summary>
        /// Gets whether this field is nullable. 
        /// </summary>
        public bool IsNullable => _f.IsNullable;

        /// <summary>
        /// Gets whether this field has a default value (including "undefined").
        /// </summary>
        public bool HasDefault => _f.HasDefault;

        /// <summary>
        /// Gets whether this field has a non null (not "undefined" for TypeScript) default.
        /// </summary>
        public bool HasNonNullDefault => _f.HasNonNullDefault;

        /// <summary>
        /// Gets or sets the documentation elements that will be written.
        /// <para>
        /// This can set to substitute a filtered or expanded set if needed. 
        /// </para>
        /// </summary>
        public IEnumerable<XElement> DocElements
        {
            get => _docs;
            set
            {
                Throw.CheckNotNullArgument( value );
                _docs = value;
            }
        }

        /// <summary>
        /// Gets or sets a documentation writer that can be used to append documentation
        /// after <see cref="DocElements"/> are written.
        /// </summary>
        public Action<DocumentationBuilder>? DocumentationExtension
        {
            get => _documentationExtension;
            set => _documentationExtension = value;
        }

        /// <summary>
        /// Gets or sets whether this field must be skipped from being written in the
        /// constructor parameters. When a field is skipped here, it must be manually
        /// defined in the <see cref="GeneratingPrimaryPocoEventArgs.BodyPart"/>.
        /// </summary>
        public bool ConstructorSkip
        {
            get => _constrctorSkip;
            set => _constrctorSkip = value;
        }

        /// <summary>
        /// Gets the field model part that can be extended.
        /// </summary>
        public ITSKeyedCodePart FieldModelPart => _fieldModelPart;

        internal void WriteFieldDefinition( TypeScriptFile file, ITSCodeWriter w )
        {
            _f.WriteCtorFieldDefinition( file, w, _docs, _documentationExtension );
        }

    }
}
