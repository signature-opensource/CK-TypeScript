using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Base class for all type of files in a <see cref="TypeScriptFolder"/>.
/// A base TypeScript file holds exported type declaration but has no parts
/// or other means to define the code itself. Available implementations are:
/// <list type="bullet">
///     <item>The regular <see cref="TypeScriptFile"/> that is used to fully generate code.</item>
///     <item>The resource based <see cref="ResourceTypeScriptFile"/> for which only TS types can be declared.</item>
/// </list>
/// </summary>
public abstract partial class TypeScriptFileBase
{
    // For virtual TypeScriptFile used by the TSTypeBuilder.
    internal static readonly string _hiddenFileName = ".hidden-file.ts";

    List<ITSDeclaredFileType>? _declaredOnlyTypes;

    readonly string _name;
    readonly TypeScriptFolder _folder;
    internal TypeScriptFileBase? _next;

    private protected TypeScriptFileBase( TypeScriptFolder folder,
                                          string name,
                                          TypeScriptFileBase? previous,
                                          bool unpublishedResourceFile )
    {
        _folder = folder;
        _name = name;
        if( !ReferenceEquals( name, _hiddenFileName ) )
        {
            bool isBarrelFile = name.Equals( "index.ts", StringComparison.OrdinalIgnoreCase );
            if( folder.IsRoot && isBarrelFile )
            {
                Throw.ArgumentException( "fileName", "Cannot create a 'index.ts' at the root (this is the default barrel)." );
            }
            if( isBarrelFile )
            {
                // While publishing, the existence of this 'index.ts' file is checked
                // and automatic generation is skipped.
                folder.EnsureBarrel();
                // We called EnsureBarrel, SetHasExportedSymbol will incremement the file count.
                folder.SetHasExportedSymbol();
            }
            else if( !unpublishedResourceFile )
            {
                // This files is published. It counts.
                folder.IncrementPublishedFileCount();
            }
            if( previous == null )
            {
                _next = folder._firstFile;
                folder._firstFile = this;
            }
            else
            {
                _next = previous._next;
                previous._next = this;
            }
        }
    }

    /// <summary>
    /// Gets the folder of this file.
    /// </summary>
    public TypeScriptFolder Folder => _folder;

    /// <inheritdoc cref="TypeScriptFolder.Root" />
    public TypeScriptRoot Root => Folder.Root;

    /// <summary>
    /// Gets this file name.
    /// It necessarily ends with '.ts' extension.
    /// </summary>
    public string Name => _name;

    /// <summary>
    /// Gets the all the TypeScript types that are defined in this file.
    /// </summary>
    public virtual IEnumerable<ITSDeclaredFileType> AllTypes => _declaredOnlyTypes ?? Enumerable.Empty<ITSDeclaredFileType>();

    /// <summary>
    /// Declares only a <see cref="ITSDeclaredFileType"/> in this file: the <paramref name="typeName"/> is implemented
    /// in this file but not in a specific <see cref="ITSCodePart"/>.
    /// <para>
    /// The <paramref name="typeName"/> must not already exist in the <see cref="TSTypeManager"/>.
    /// </para>
    /// </summary>
    /// <param name="typeName">The TypeScript type name.</param>
    /// <param name="additionalImports">The required imports. Null when using this type requires only this file.</param>
    /// <param name="defaultValueSource">The type default value if any.</param>
    /// <returns>A TS type in this file (but with no associated <see cref="ITSCodePart"/>).</returns>
    public ITSDeclaredFileType DeclareType( string typeName,
                                            Action<ITSFileImportSection>? additionalImports = null,
                                            string? defaultValueSource = null )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( typeName );
        Throw.CheckArgument( "Expected single type name. Commas or semicolons are not supported.",
                             typeName.AsSpan().ContainsAny( ',', ';' ) is false );
        var t = new TSDeclaredType( this, typeName, additionalImports, defaultValueSource );
        _declaredOnlyTypes ??= new List<ITSDeclaredFileType>();
        _declaredOnlyTypes.Add( t );
        return t;
    }

    /// <summary>
    /// Must return the full content of this file.
    /// </summary>
    /// <param name="monitor">Required monitor for warnings when types from '@local/ck-gen' cannot be resolved.</param>
    /// <param name="tsTypes">The type manager required to handle imports from '@local/ck-gen'.</param>
    /// <returns>This file's content.</returns>
    public abstract string GetCurrentText( IActivityMonitor monitor, TSTypeManager tsTypes );

    /// <summary>
    /// Overridden to return this file name.
    /// </summary>
    /// <returns>The <see cref="Name"/>.</returns>
    public override string ToString() => Name;

}

