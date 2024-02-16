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
    /// <para>
    /// This is wrapper around the <see cref="TSField"/> value that adds mutable <see cref="ConstructorSkip"/>,
    /// <see cref="DocElements"/> and <see cref="DocumentationExtension"/> properties.
    /// </para>
    /// </summary>
    public sealed class TSNamedCompositeField
    {
        readonly TSField _f;
        IEnumerable<XElement> _docs;
        Action<DocumentationBuilder>? _documentationExtension;
        bool _constrctorSkip;

        internal TSNamedCompositeField( TSField f )
        {
            _f = f;
            _docs = _f.Docs;
        }

        /// <summary>
        /// Gets the TSField.
        /// </summary>
        public TSField TSField => _f;

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
        /// initialized in the <see cref="GeneratingPrimaryPocoEventArgs.BodyPart"/>.
        /// </summary>
        public bool ConstructorSkip
        {
            get => _constrctorSkip;
            set => _constrctorSkip = value;
        }

        internal void WriteCtorFieldDefinition( TypeScriptFile file, ITSCodeWriter w )
        {
            _f.WriteCtorFieldDefinition( file, w, _docs, _documentationExtension );
        }

    }
}
