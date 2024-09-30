using CK.Core;
using CK.TypeScript.CodeGen;
using CSemVer;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CK.Setup;

/// <summary>
/// package.json file model.
/// </summary>
public sealed class PackageJsonFile 
{
    JsonFile _file;
    readonly DependencyCollection _dependencies;
    readonly Dictionary<string, string> _scripts;
    HashSet<string>? _workspaces;
    string? _name;
    SVersion? _version;
    string? _module;
    string? _main;
    int _ckVersion;
    bool? _private;

    PackageJsonFile( JsonFile f, DependencyCollection dependencies )
    {
        _file = f;
        _dependencies = dependencies;
        _scripts = new Dictionary<string, string>();
    }

    /// <summary>
    /// Tries to read a package.json file or creates a new empty one.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="filePath">The file path. Must be <see cref="NormalizedPath.IsRooted"/>.</param>
    /// <param name="ignoreVersionsBound">See <see cref="LibraryManager.IgnoreVersionsBound"/>.</param>
    /// <param name="mustExist">
    /// True to emit an error and return null if the file doesn't exist.
    /// By default, an empty PackageJsonFile is returned if there's no file.
    /// </param>
    /// <returns>The <see cref="PackageJsonFile"/> or null on error.</returns>
    public static PackageJsonFile? ReadFile( IActivityMonitor monitor,
                                             NormalizedPath filePath,
                                             bool ignoreVersionsBound,
                                             bool mustExist = false )
    {
        var nf = JsonFile.ReadFile( monitor, filePath, mustExist );
        if( !nf.HasValue ) return null;
        var f = new PackageJsonFile( nf.Value, new DependencyCollection( ignoreVersionsBound ) );
        return f.DoRead( monitor ) ? f : null;
    }

    /// <summary>
    /// Mainly for tests.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="content">The json content to parse. Must be a valid object.</param>
    /// <param name="filePath">The file path. Must be <see cref="NormalizedPath.IsRooted"/>.</param>
    /// <param name="ignoreVersionsBound">See <see cref="LibraryManager.IgnoreVersionsBound"/>.</param>
    /// <returns>The <see cref="PackageJsonFile"/> or null on error.</returns>
    public static PackageJsonFile? Parse( IActivityMonitor monitor,
                                          string content,    
                                          NormalizedPath filePath,
                                          bool ignoreVersionsBound )
    {
        Throw.CheckArgument( filePath.Parts.Count >= 2 && filePath.IsRooted );
        var documentOptions = new JsonDocumentOptions{ AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };
        JsonNode? root = JsonNode.Parse( content, nodeOptions: default, documentOptions );
        Throw.CheckArgument( "Invalid content string.", root != null && root is JsonObject );
        var f = new PackageJsonFile( new JsonFile( (JsonObject)root, filePath ), new DependencyCollection( ignoreVersionsBound ) );
        return f.DoRead( monitor ) ? f : null;
    }

    /// <summary>
    /// Reloads the file.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <returns>True on success, false on error.</returns>
    public bool Reload( IActivityMonitor monitor )
    {
        var nf = JsonFile.ReadFile( monitor, _file.FilePath, mustExist: false );
        if( !nf.HasValue ) return false;
        _file = nf.Value;
        return DoRead( monitor );
    }

    bool DoRead( IActivityMonitor monitor )
    {
        var success = _file.ReadStringDictionary( _file.Root, monitor, "scripts", out var scripts );
        success &= _file.ReadStringList( _file.Root, monitor, "workspaces", out var workspaces );
        var dependencies = ReadDependencies( _file, monitor, _dependencies.IgnoreVersionsBound );
        success &= dependencies != null;

        success &= _file.GetNonNullJsonString( _file.Root, monitor, "name", out var name );
        success &= _file.GetNonNullJsonNumber( _file.Root, monitor, "ckVersion", out var ckVersion );

        SVersion? version = null;
        if( _file.GetNonNullJsonString( _file.Root, monitor, "version", out var sVersion ) )
        {
            if( sVersion != null && !SVersion.TryParse( sVersion, out version ) )
            {
                monitor.Error( $"Unable to parse \"version\" property." );
                success = false;
            }
        }
        else
        {
            success = false;
        }
        success &= _file.GetNonNullJsonString( _file.Root, monitor, "module", out var module );
        success &= _file.GetNonNullJsonString( _file.Root, monitor, "main", out var main );
        success &= _file.GetNonNullJsonBoolean( _file.Root, monitor, "private", out bool? isPrivate );

        if( !success )
        {
            monitor.Error( $"Unable to read file '{_file.FilePath}'." );
            return false;
        }
        _scripts.Clear();
        if( scripts != null ) _scripts.AddRange( scripts );
        _workspaces = workspaces != null ? new HashSet<string>( workspaces ) : null;
        _dependencies.Clear();
        _dependencies.AddOrUpdate( monitor, dependencies!.Values );
        _name = name;
        _version = version;
        _module = module;
        _main = main;
        _ckVersion = ckVersion.HasValue ? (int)ckVersion : 0;
        _private = isPrivate;
        return true;

        static DependencyCollection? ReadDependencies( JsonFile file, IActivityMonitor monitor, bool ignoreVersionsBound )
        {
            var collector = new DependencyCollection( ignoreVersionsBound );
            if( Read( monitor, file, DependencyKind.Dependency, collector )
                && Read( monitor, file, DependencyKind.DevDependency, collector )
                && Read( monitor, file, DependencyKind.PeerDependency, collector ) )
            {
                return collector;
            }
            return null;

            static bool Read( IActivityMonitor monitor, JsonFile file, DependencyKind kind, DependencyCollection collector )
            {
                string sectionName = kind.GetJsonSectionName();
                if( !file.GetNonJsonNull( file.Root, monitor, sectionName, out JsonObject? section ) )
                {
                    return false;
                }
                bool success = true;
                if( section != null )
                {
                    foreach( var (name, sV) in section )
                    {
                        if( string.IsNullOrWhiteSpace( name ) ) continue;
                        var v = TryParse( sV, out var error );
                        if( error != null )
                        {
                            monitor.Error( $"Unable to parse version \"{sectionName}.{name}\": {error}" );
                            success = false;
                        }
                        else if( !collector.AddOrUpdate( monitor, new PackageDependency( name, v, kind ) ) )
                        {
                            success = false;
                        }
                    }
                }
                return success;
            }

            static SVersionBound TryParse( JsonNode? sV, out string? error )
            {
                if( sV is JsonValue v && v.TryGetValue( out string? s ) )
                {
                    // These are external libraries. Prerelease versions have not the same semantics as our in the npm
                    // ecosystem. We use the mainstream semantics here.
                    if( s.StartsWith( "workspace:", StringComparison.OrdinalIgnoreCase ) )
                    {
                        error = null;
                        return SVersionBound.None;
                    }
                    var parseResult = SVersionBound.NpmTryParse( s, includePrerelease: false );
                    if( parseResult.IsValid )
                    {
                        error = null;
                        return parseResult.Result;
                    }
                    Throw.DebugAssert( parseResult.Error != null );
                    error = parseResult.Error;
                }
                else
                {
                    error = "is not a string.";
                }
                return SVersionBound.None;
            }
        }
    }

    /// <summary>
    /// Creates a new empty package.json file.
    /// </summary>
    /// <param name="filePath">The file path. Must be <see cref="NormalizedPath.IsRooted"/>.</param>
    /// <param name="ignoreVersionsBound">See <see cref="LibraryManager.IgnoreVersionsBound"/>.</param>
    /// <returns>An empty package.json file.</returns>
    public static PackageJsonFile CreateEmpty( NormalizedPath filePath, bool ignoreVersionsBound )
    {
        Throw.CheckArgument( filePath.Parts.Count >= 3 && filePath.IsRooted );
        return new PackageJsonFile( new JsonFile( new JsonObject(), filePath ), new DependencyCollection( ignoreVersionsBound ) );
    }

    /// <summary>
    /// Creates a new package.json file with dependencies.
    /// </summary>
    /// <param name="filePath">The file path. Must be <see cref="NormalizedPath.IsRooted"/>.</param>
    /// <param name="dependencies">Package dependencies.</param>
    /// <returns>An empty package.json file.</returns>
    public static PackageJsonFile Create( NormalizedPath filePath,
                                          DependencyCollection dependencies )
    {
        Throw.CheckArgument( filePath.Parts.Count >= 3 && filePath.IsRooted );
        return new PackageJsonFile( new JsonFile( new JsonObject(), filePath ), dependencies );
    }

    /// <summary>
    /// Gets the rooted file path.
    /// </summary>
    public NormalizedPath FilePath => _file.FilePath;

    /// <summary>
    /// Gets whether this package.json is empty.
    /// </summary>
    public bool IsEmpty => _file.Root.Count == 0;

    /// <summary>
    /// Gets or sets the "name".
    /// Can be null: it will be removed from the json when saved.
    /// </summary>
    public string? Name { get => _name; set => _name = value; }

    /// <summary>
    /// Gets this name or, when <see cref="Name"/> is null or whitespace, the folder's name in lowercase.
    /// </summary>
    public string SafeName => string.IsNullOrWhiteSpace( _name )
                                ? _file.FilePath.Parts[^2].ToLowerInvariant()
                                : _name;

    /// <summary>
    /// Gets the all the dependencies from the "devDependencies", "dependencies" and "peerDependencies" properties.
    /// <para>
    /// When the same package appear in more than one section, the unique final <see cref="PackageDependency"/> is merged:
    /// <list type="bullet">
    ///     <item>Peer dependencies win over regular that win over developpement dependencies.</item>
    ///     <item>Final version is upgraded.</item>
    /// </list>
    /// </para>
    public DependencyCollection Dependencies => _dependencies;

    /// <summary>
    /// Gets the mutable "scripts".
    /// </summary>
    public Dictionary<string, string> Scripts => _scripts;

    /// <summary>
    /// Gets or sets the mutable "workspaces".
    /// <para>
    /// When null, no "workspaces" property appears.
    /// </para>
    /// </summary>
    public HashSet<string>? Workspaces
    {
        get => _workspaces;
        set => _workspaces = value;
    }

    /// <summary>
    /// Ensures that the <paramref name="workspace"/> appears in <see cref="Workspaces"/>.
    /// </summary>
    /// <param name="workspace">The workspace name.</param>
    /// <returns>True if the workspace has been added, false it it already exists.</returns>
    public bool EnsureWorkspace( string workspace )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( workspace );
        _workspaces ??= new HashSet<string>();
        return _workspaces.Add( workspace );
    }

    /// <summary>
    /// Gets or sets the package "version".
    /// Can be null: it will be removed from the json when saved.
    /// </summary>
    public SVersion? Version { get => _version; set => _version = value; }

    /// <summary>
    /// Gets or sets the "module" (ESM).
    /// </summary>
    public string? Module { get => _module; set => _module = value; }

    /// <summary>
    /// Gets or sets the "main" (CJS).
    /// </summary>
    public string? Main { get => _main; set => _main = value; }

    /// <summary>
    /// Gets or sets the "private".
    /// </summary>
    public bool? Private { get => _private; set => _private = value; }

    /// <summary>
    /// Gets or sets the optional "ckVersion".
    /// This defaults to 0 (i.e. the property is missing).
    /// </summary>
    public int CKVersion { get => _ckVersion; set => _ckVersion = value; }

    /// <summary>
    /// Updates the inner <see cref="JsonFile.Root"/>.
    /// </summary>
    /// <param name="peerDependenciesAsDepencies">
    /// True to also add <see cref="DependencyKind.PeerDependency"/> to the regular "dependecies" section.
    /// </param>
    public void UpdateFileRoot( bool peerDependenciesAsDepencies = true )
    {
        _file.SetString( _file.Root, "name", _name );
        _file.SetString( _file.Root, "version", _version?.ToString() );
        _file.SetString( _file.Root, "module", _module );
        _file.SetString( _file.Root, "main", _main );
        _file.SetBoolean( _file.Root, "private", _private );
        _file.SetNumber( _file.Root, "ckVersion", _ckVersion != 0 ? _ckVersion : null );
        _file.SetStringDictionary( _file.Root, "scripts", _scripts );
        _file.SetStringList( _file.Root, "workspaces", _workspaces );
        SetDependencies( _file.Root, _dependencies, peerDependenciesAsDepencies );


        static void SetDependencies( JsonNode root, DependencyCollection dependencies, bool peerDependenciesAsDepencies = true )
        {
            var deps = new[] { new JsonObject(), new JsonObject(), new JsonObject() };
            foreach( var d in dependencies.Values )
            {
                var version = d.NpmVersionRange;
                deps[(int)d.DependencyKind].Add( d.Name, version );
                if( peerDependenciesAsDepencies && d.DependencyKind == DependencyKind.PeerDependency )
                {
                    deps[1].Add( d.Name, version );
                }
            }
            root["devDependencies"] = deps[0];
            root["dependencies"] = deps[1];
            root["peerDependencies"] = deps[2];
        }
    }

    /// <summary>
    /// Calls <see cref="UpdateFileRoot"/> and saves the <see cref="FilePath"/> file.
    /// </summary>
    /// <param name="peerDependenciesAsDepencies">
    /// True to also add <see cref="DependencyKind.PeerDependency"/> to the regular "dependecies" section.
    /// </param>
    public void Save( bool peerDependenciesAsDepencies = true )
    {
        using var f = File.Create( _file.FilePath );
        Write( f, peerDependenciesAsDepencies );
    }

    /// <summary>
    /// Calls <see cref="UpdateFileRoot"/> and writes this package.json to the output.
    /// </summary>
    /// <param name="output">Target outpout.</param>
    /// <param name="peerDependenciesAsDepencies">
    /// True to also add <see cref="DependencyKind.PeerDependency"/> to the regular "dependecies" section.
    /// </param>
    public void Write( Stream output, bool peerDependenciesAsDepencies = true )
    {
        UpdateFileRoot( peerDependenciesAsDepencies );
        using var writer = new Utf8JsonWriter( output, new JsonWriterOptions { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping } );
        _file.Root.WriteTo( writer );
    }

    /// <summary>
    /// Calls <see cref="UpdateFileRoot"/> and writes this package.json to a string.
    /// <para>
    /// This class doesn't override ToString() because UpdateContent has to be called first to reflect
    /// the changes.
    /// </para>
    /// </summary>
    /// <param name="peerDependenciesAsDepencies">
    /// True to also add <see cref="DependencyKind.PeerDependency"/> to the regular "dependecies" section.
    /// </param>
    public string WriteAsString( bool peerDependenciesAsDepencies = true )
    {
        UpdateFileRoot( peerDependenciesAsDepencies );
        var a = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter( a, new JsonWriterOptions { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping } );
        _file.Root.WriteTo( writer );
        writer.Flush();
        return Encoding.UTF8.GetString( a.WrittenSpan );
    }
}