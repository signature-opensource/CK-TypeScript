using CK.Core;
using CK.EmbeddedResources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Base class for all type of files in a <see cref="TypeScriptFolder"/>.
/// A base TypeScript file holds exported type declaration but has no parts
/// or other means to define the code itself. Available implementations are:
/// <list type="bullet">
///     <item>The regular <see cref="TypeScriptFile"/> that is used to fully generate code.</item>
///     <item>The resource based <see cref="ResourceTypeScriptFile"/>.</item>
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

    private protected TypeScriptFileBase( TypeScriptFolder folder, string name, TypeScriptFileBase? previous )
    {
        _folder = folder;
        _name = name;
        if( !ReferenceEquals( name, _hiddenFileName ) )
        {
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
        _declaredOnlyTypes ??= new List<ITSDeclaredFileType>();
        var t = new TSDeclaredType( this, typeName, additionalImports, defaultValueSource );
        _declaredOnlyTypes.Add( t );
        return t;
    }

    /// <summary>
    /// Must return the full content if this file.
    /// </summary>
    /// <returns>This file's content.</returns>
    public abstract string GetCurrentText();

    /// <summary>
    /// Gets this file path (not prefixed by '/').
    /// </summary>
    public string FilePath => _folder.Path.IsEmptyPath ? _name : _folder.Path.Path + '/' + _name;

    /// <summary>
    /// Saves this file into a folder on the file system.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="saver">The <see cref="TypeScriptFileSaveStrategy"/>.</param>
    public void Save( IActivityMonitor monitor, TypeScriptFileSaveStrategy saver )
    {
        var filePath = saver._currentTarget.AppendPart( Name );
        saver.SaveFile( monitor, this, filePath );
    }

    /// <summary>
    /// Overridden to return this file name.
    /// </summary>
    /// <returns>The <see cref="Name"/>.</returns>
    public override string ToString() => Name;

}

