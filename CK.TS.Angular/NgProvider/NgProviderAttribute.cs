using CK.Core;
using CK.Setup;
using System;

namespace CK.TS.Angular;

/// <summary>
/// Non generic base class <see cref="NgProviderAttribute{T}"/>
/// </summary>
public abstract class NgProviderAttribute : ContextBoundDelegationAttribute, IRealObjectAttribute
{
    private protected NgProviderAttribute( Type typeScriptPackage, (string Code, string? Source)[] definitions  )
        : base( "CK.TS.Angular.Engine.NgProviderAttributeImpl, CK.TS.Angular.Engine" )
    {
        TypeScriptPackage = typeScriptPackage;
        ProviderDefinitions = definitions;
    }

    /// <summary>
    /// Gets the <see cref="TypeScriptPackage"/> to which this provider belongs.
    /// </summary>
    public Type TypeScriptPackage { get; }

    /// <summary>
    /// Gets the providers definition code and optional source.
    /// </summary>
    public (string Code, string? SourceName)[] ProviderDefinitions { get; }

    Type? IRealObjectAttribute.Container => TypeScriptPackage;

    DependentItemKindSpec IRealObjectAttribute.ItemKind => DependentItemKindSpec.Item;

    TrackAmbientPropertiesMode IRealObjectAttribute.TrackAmbientProperties => TrackAmbientPropertiesMode.None;

    Type[]? IRealObjectAttribute.Requires => Type.EmptyTypes;

    Type[]? IRealObjectAttribute.RequiredBy => Type.EmptyTypes;

    Type[]? IRealObjectAttribute.Children => Type.EmptyTypes;

    Type[]? IRealObjectAttribute.Groups => Type.EmptyTypes;
}
