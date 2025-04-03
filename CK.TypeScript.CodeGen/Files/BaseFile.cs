using CK.Core;
using CK.EmbeddedResources;
using System;
using System.IO;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Base class for all type of files in a <see cref="TypeScriptFolder"/>.
/// </summary>
public abstract class BaseFile
{
    // For virtual TypeScriptFile used by the TSTypeBuilder.
    internal static readonly string _hiddenFileName = ".hidden-file.ts";

    readonly string _name;
    readonly TypeScriptFolder _folder;
    internal BaseFile? _next;

    private protected BaseFile( TypeScriptFolder folder, string name )
    {
        _folder = folder;
        _name = name;
        if( !ReferenceEquals( name, _hiddenFileName ) )
        {
            _next = folder._firstFile;
            folder._firstFile = this;
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
    /// It necessarily ends with the proper extension ('.ts' for a <see cref="TypeScriptFile"/>).
    /// </summary>
    public string Name => _name;

    /// <summary>
    /// Gets this file path (not prefixed by '/').
    /// </summary>
    public string FilePath => _folder.Path.IsEmptyPath ? _name : _folder.Path.Path + '/' + _name;

    /// <summary>
    /// Gets the file name extension including the leading dot.
    /// </summary>
    public ReadOnlySpan<char> Extension => Path.GetExtension( _name.AsSpan() );

    /// <summary>
    /// Gets whether this file holds a stream (<see cref="GetStream()"/> and <see cref="WriteStream()"/> can be called)
    /// or the content must be obtained by other methods specific to the actual type.
    /// </summary>
    public abstract bool HasStream { get; }

    /// <summary>
    /// Gets the content of this file as a stream (that must be disposed once done with it).
    /// <see cref="HasStream"/> must be true otherwise an <see cref="InvalidOperationException"/> is thrown.
    /// </summary>
    /// <returns>The content stream.</returns>
    public abstract Stream GetStream();

    /// <summary>
    /// Writes the content of this file to the target.
    /// <see cref="HasStream"/> must be true otherwise an <see cref="InvalidOperationException"/> is thrown.
    /// </summary>
    /// <param name="target">The target stream to write to.</param>
    public abstract void WriteStream( Stream target );

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


    internal static BaseFile CreateResourceFile( TypeScriptFolder folder, string name, in ResourceLocator locator )
    {
        Throw.DebugAssert( folder != null && name != null );
        var n = Path.GetExtension( name.AsSpan() );
        return n switch
        {
            ".ts" => new ResourceTypeScriptFile( folder, name, locator ),
            ".htm" or ".html" => new ResourceHtmlFile( folder, name, locator ),
            ".less" or ".css" => new ResourceStyleFile( folder, name, locator ),
            ".json" or ".jsonc" => new ResourceJsonFile( folder, name, locator ),
            ".txt" or ".text" or ".md" => new ResourceTextFile( folder, name, locator ),
            ".js" => new ResourceJavaScriptFile( folder, name, locator ),
            _ => new ResourceUnknownFile( folder, name, locator ),
        };
    }

}

