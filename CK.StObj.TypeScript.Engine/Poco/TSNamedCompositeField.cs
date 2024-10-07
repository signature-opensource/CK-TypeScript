using CK.Core;
using CK.StObj.TypeScript.Engine;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace CK.Setup;

/// <summary>
/// Expose Poco fields that will appear in the constructor, allowing to alter its documentation.
/// <para>
/// This is wrapper around the <see cref="TSField"/> value that adds mutable 
/// <see cref="DocElements"/> and <see cref="DocumentationExtension"/> properties.
/// </para>
/// </summary>
public sealed class TSNamedCompositeField
{
    readonly TSField _f;
    IEnumerable<XElement> _docs;
    Action<DocumentationBuilder>? _documentationExtension;

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


    internal void WriteCtorFieldDefinition( ITSCodeWriter w )
    {
        _f.WriteFieldDefinition( w, true, _docs, _documentationExtension );
    }

    internal void WriteFieldDefinition( ITSCodeWriter w )
    {
        _f.WriteFieldDefinition( w, false, _docs, _documentationExtension );
    }

}
