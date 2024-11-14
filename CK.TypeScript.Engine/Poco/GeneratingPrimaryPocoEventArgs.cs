using CK.Core;
using CK.TypeScript.Engine;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;

namespace CK.Setup;

/// <summary>
/// Raised by <see cref="PocoCodeGenerator.PrimaryPocoGenerating"/>.
/// This event enable participants to alter the TypeScript Poco code.
/// </summary>
public sealed class GeneratingPrimaryPocoEventArgs : EventMonitoredArgs
{
    readonly TypeScriptContext _typeScriptContext;
    readonly ITSFileCSharpType _tsType;
    readonly IPrimaryPocoType _pocoType;
    readonly ImmutableArray<TSNamedCompositeField> _fields;
    readonly ITSCodePart _interfacesPart;
    readonly ITSCodePart _fieldDefinitionPart;
    readonly ITSCodePart _ctorParametersPart;
    readonly ITSCodePart _ctorBodyPart;
    IEnumerable<Type> _docTypes;
    IEnumerable<IAbstractPocoType> _implementedInterfaces;
    Action<DocumentationBuilder>? _documentationExtension;

    internal GeneratingPrimaryPocoEventArgs( IActivityMonitor monitor,
                                             TypeScriptContext typeScriptContext,
                                             ITSFileCSharpType tSGeneratedType,
                                             IPrimaryPocoType pocoType,
                                             IEnumerable<IAbstractPocoType> implementedInterfaces,
                                             ImmutableArray<TSNamedCompositeField> fields,
                                             ITSCodePart interfacesPart,
                                             ITSCodePart fieldDefinitionPart,
                                             ITSCodePart ctorParametersPart,
                                             ITSCodePart ctorBodyPart )
        : base( monitor )
    {
        _typeScriptContext = typeScriptContext;
        _tsType = tSGeneratedType;
        _pocoType = pocoType;
        _docTypes = pocoType.SecondaryTypes.Select( s => s.Type ).Prepend( pocoType.Type );
        _implementedInterfaces = implementedInterfaces;
        _fields = fields;
        _interfacesPart = interfacesPart;
        _fieldDefinitionPart = fieldDefinitionPart;
        _ctorParametersPart = ctorParametersPart;
        _ctorBodyPart = ctorBodyPart;
    }

    /// <summary>
    /// Gets the context.
    /// </summary>
    public TypeScriptContext TypeScriptContext => _typeScriptContext;

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
    /// Gets the field definition part.
    /// </summary>
    public ITSCodePart FieldDefinitionPart => _fieldDefinitionPart;

    /// <summary>
    /// Gets the exposed constructor parameters part. It will be filled with the <see cref="Fields"/>
    /// all marked as optional and their non nullable type.
    /// </summary>
    public ITSCodePart CtorParametersPart => _ctorParametersPart;

    /// <summary>
    /// Gets the constructor body part. It will be filled with <see cref="Fields"/> assignation (from <see cref="CtorParametersPart"/>)
    /// with the ?? field's default value when the field has a default value.
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
    public ImmutableArray<TSNamedCompositeField> Fields => _fields;

    /// <summary>
    /// Gets the part to extend the poco class.
    /// </summary>
    public ITSCodePart PocoTypePart => _tsType.TypePart;

}
