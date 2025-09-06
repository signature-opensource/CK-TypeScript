using CK.Core;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CK.Setup;

/// <summary>
/// tsConfig.json file model.
/// <para>
/// This totally hide the underlying Json implementation (currently this uses the System.Text.Json.Nodes
/// objects that is far from perfect): this may change.
/// The major drawback is that only what is exposed can be altered (and this is also not ideal).  
/// </para>
/// </summary>
public sealed class TSConfigJsonFile
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public const bool DefaultStrict = true;
    public const bool DefaultNoImplicitOverride = true;
    public const bool DefaultNoPropertyAccessFromIndexSignature = true;
    public const bool DefaultNoImplicitReturns = true;
    public const bool DefaultNoFallthroughCasesInSwitch = true;

    public const bool DefaultSkipLibCheck = true;
    public const bool DefaultAllowSyntheticDefaultImports = true;
    public const bool DefaultDeclaration = false;
    public const bool DefaultDeclarationMap = false;
    public const bool DefaultESModuleInterop = true;
    public const bool DefaultSourceMap = false;
    public const bool DefaultResolveJsonModule = true;

    public const string DefaultTarget = "es2022";
    public const string DefaultModule = "NodeNext";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    JsonFile _file;
    readonly NormalizedPath _folderPath;
    [AllowNull] JsonObject _compilerOptions;
    // A missing CompilerOptions.types is not the same as an empty one.
    List<string>? _coTypes;
    // A missing CompilerOptions.path is not the same as an empty one.
    Dictionary<string, HashSet<string>>? _coPaths;
    NormalizedPath _baseUrl;
    // These are compiler options.
    string _target;
    string _module;
    bool _strict;
    bool _noImplicitOverride;
    bool _noPropertyAccessFromIndexSignature;
    bool _noImplicitReturns;
    bool _noFallthroughCasesInSwitch;
    bool _skipLibCheck;
    bool _allowSyntheticDefaultImports;
    bool _declaration;
    bool _declarationMap;
    bool _esModuleInterop;
    bool _sourceMap;
    bool _resolveJsonModule;

    TSConfigJsonFile( JsonFile f )
    {
        _file = f;
        _folderPath = f.FilePath.RemoveLastPart();
        _strict = DefaultStrict;
        _noImplicitOverride = DefaultNoImplicitOverride;
        _noFallthroughCasesInSwitch = DefaultNoFallthroughCasesInSwitch;
        _noImplicitReturns = DefaultNoImplicitReturns;
        _noPropertyAccessFromIndexSignature = DefaultNoPropertyAccessFromIndexSignature;
        _skipLibCheck = DefaultSkipLibCheck;
        _allowSyntheticDefaultImports = DefaultAllowSyntheticDefaultImports;
        _declaration = DefaultDeclaration;
        _declarationMap = DefaultDeclarationMap;
        _esModuleInterop = DefaultESModuleInterop;
        _sourceMap = DefaultSourceMap;
        _target = DefaultTarget;
        _module = DefaultModule;
        _resolveJsonModule = DefaultResolveJsonModule;
    }

    /// <summary>
    /// If <see cref="IsEmpty"/> is true, this updates the underlying Json with:
    /// <code>
    /// {
    ///     "compilerOptions": {
    ///         "strict": true,
    ///         "noFallthroughCasesInSwitch": true,
    ///         "noFallthroughCasesInSwitch": true,
    ///         "noImplicitOverride": true,
    ///         "noImplicitReturns": true,
    ///         "noPropertyAccessFromIndexSignature": true,
    ///         "skipLibCheck": true,
    ///         "allowSyntheticDefaultImports": true,
    ///         "declaration": false,
    ///         "esModuleInterop": true,
    ///         "sourceMap": false,
    ///         "target": "es2022",
    ///         "module": "NodeNext",
    ///         "moduleResolution": "NodeNext"
    ///     }
    /// }
    /// </code>
    /// Note that the actual values update in the underlying Json are the <see cref="CompileOptionsStrict"/>,
    /// <see cref="CompileOptionsNoFallthroughCasesInSwitch"/> etc. current values.
    /// </summary>
    /// <param name="monitor">The monitor.</param>
    /// <returns>True if this was empty and minimal defaults have been set, false otherwise.</returns>
    public bool EnsureDefault( IActivityMonitor monitor )
    {
        if( IsEmpty )
        {
            UpdateSimpleCompilerOptions();
            monitor.Trace( $"Ensuring tsconfig.json default '{_file.FilePath}'." );
            return true;
        }
        return false;
    }

    /// <summary>
    /// Tries to read a tsConfig.json file or creates a new empty one.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="filePath">The file path. Must be <see cref="NormalizedPath.IsRooted"/>.</param>
    /// <param name="mustExist">
    /// True to emit an error and return null if the file doesn't exist.
    /// By default, an empty TSConfigJsonFile is returned if there's no file.
    /// </param>
    /// <returns>The <see cref="TSConfigJsonFile"/> or null on error.</returns>
    public static TSConfigJsonFile? ReadFile( IActivityMonitor monitor,
                                              NormalizedPath filePath,
                                              bool mustExist = false )
    {
        var nf = JsonFile.ReadFile( monitor, filePath, mustExist );
        if( !nf.HasValue ) return null;
        var f = new TSConfigJsonFile( nf.Value );
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
        bool success = _file.GetNonJsonNull<JsonObject>( _file.Root, monitor, "compilerOptions", out var compilerOptions );
        if( compilerOptions == null )
        {
            compilerOptions = new JsonObject();
            _file.Root.Add( "compilerOptions", compilerOptions );
        }
        success &= _file.GetNonNullJsonBoolean( compilerOptions, monitor, "strict", out var strict );
        success &= _file.GetNonNullJsonBoolean( compilerOptions, monitor, "noFallthroughCasesInSwitch", out var noFallthroughCasesInSwitch );
        success &= _file.GetNonNullJsonBoolean( compilerOptions, monitor, "noImplicitOverride", out var noImplicitOverride );
        success &= _file.GetNonNullJsonBoolean( compilerOptions, monitor, "noPropertyAccessFromIndexSignature", out var noPropertyAccessFromIndexSignature );
        success &= _file.GetNonNullJsonBoolean( compilerOptions, monitor, "noImplicitReturns", out var noImplicitReturns );

        success &= _file.GetNonNullJsonBoolean( compilerOptions, monitor, "skipLibCheck", out var skipLibCheck );
        success &= _file.GetNonNullJsonBoolean( compilerOptions, monitor, "allowSyntheticDefaultImports", out var allowSyntheticDefaultImports );
        success &= _file.GetNonNullJsonBoolean( compilerOptions, monitor, "declaration", out var declaration );
        success &= _file.GetNonNullJsonBoolean( compilerOptions, monitor, "declarationMap", out var declarationMap );
        success &= _file.GetNonNullJsonBoolean( compilerOptions, monitor, "esModuleInterop", out var esModuleInterop );
        success &= _file.GetNonNullJsonBoolean( compilerOptions, monitor, "sourceMap", out var sourceMap );
        success &= _file.GetNonNullJsonBoolean( compilerOptions, monitor, "resolveJsonModule", out var resolveJsonModule );

        success &= _file.GetNonNullJsonString( compilerOptions, monitor, "target", out var target );
        success &= _file.GetNonNullJsonString( compilerOptions, monitor, "module", out var module );
        success &= _file.GetNonNullJsonString( compilerOptions, monitor, "moduleResolution", out var moduleResolution );

        success &= _file.GetNonNullJsonString( compilerOptions, monitor, "baseUrl", out var baseUrl );
        success &= ReadPaths( _file, compilerOptions, monitor, out var paths );
        success &= _file.ReadStringList( compilerOptions, monitor, "types", out var coTypes );
        if( !success )
        {
            monitor.Error( $"Unable to read file '{_file.FilePath}'." );
            return false;
        }
        _strict = strict ?? DefaultStrict;
        _noFallthroughCasesInSwitch = noFallthroughCasesInSwitch ?? DefaultNoFallthroughCasesInSwitch;
        _noImplicitOverride = noImplicitOverride ?? DefaultNoImplicitOverride;
        _noPropertyAccessFromIndexSignature = noPropertyAccessFromIndexSignature ?? DefaultNoPropertyAccessFromIndexSignature;
        _noImplicitReturns = noImplicitReturns ?? DefaultNoImplicitReturns;

        _skipLibCheck = skipLibCheck ?? DefaultSkipLibCheck;
        _allowSyntheticDefaultImports = allowSyntheticDefaultImports ?? DefaultAllowSyntheticDefaultImports;
        _declaration = declaration ?? DefaultDeclaration;
        _declarationMap = declarationMap ?? DefaultDeclarationMap;
        _esModuleInterop = esModuleInterop ?? DefaultESModuleInterop;
        _sourceMap = sourceMap ?? DefaultSourceMap;

        _target = target ?? DefaultTarget;
        _module = module ?? DefaultModule;

        _compilerOptions = compilerOptions;
        _baseUrl = baseUrl;
        _coPaths = paths;
        _coTypes = coTypes;
        _resolveJsonModule = resolveJsonModule ?? DefaultResolveJsonModule;
        return true;
    }

    /// <summary>
    /// Gets the rooted file path.
    /// </summary>
    public NormalizedPath FilePath => _file.FilePath;

    /// <summary>
    /// Gets whether this package.json is empty.
    /// </summary>
    public bool IsEmpty => _file.Root.Count == 1 && _compilerOptions.Count == 0;

    /// <summary>
    /// Gets or sets the "baseUrl".
    /// When set to empty, it will be removed from the json when saved.
    /// </summary>
    public NormalizedPath BaseUrl { get => _baseUrl; set => _baseUrl = value; }

    /// <summary>
    /// Gets the <see cref="FilePath"/>'s directory combined with the <see cref="BaseUrl"/>.
    /// </summary>
    public NormalizedPath ResolvedBaseUrl => _folderPath.Combine( _baseUrl ).ResolveDots();

    /// <summary>
    /// Gets or sets the "compilerOptions": { "types": [ ... ] }.
    /// Null when types property is missing.
    /// </summary>
    public List<string>? CompilerOptionsTypes
    {
        get => _coTypes;
        set => _coTypes = value;
    }

    /// <summary>
    /// Gets or sets the "compilerOptions": { "paths": { "key", ["v1","v2"] } } paths mappings.
    /// Null when paths property is missing.
    /// Uses <see cref="CompileOptionsPathEnsureMapping(string, string)"/> to easily add entries
    /// to it.
    /// </summary>
    public Dictionary<string, HashSet<string>>? CompilerOptionsPaths
    {
        get => _coPaths;
        set => _coPaths = value;
    }

    /// <summary>
    /// Ensures that "compilerOptions": { "paths": { "from", ["to","other"...] } } exists. 
    /// </summary>
    /// <param name="from">The key.</param>
    /// <param name="to">The mapping.</param>
    /// <returns>True if the mapping has been added, false if it already exists.</returns>
    public bool CompileOptionsPathEnsureMapping( string from, string to )
    {
        _coPaths ??= new Dictionary<string, HashSet<string>>();
        if( !_coPaths.TryGetValue( from, out var mappings ) )
        {
            mappings = new HashSet<string> { to };
            _coPaths.Add( from, mappings );
            return true;
        }
        return mappings.Add( to );
    }

    /// <summary>
    /// Defaults to <see cref="DefaultStrict"/>.
    /// </summary>
    public bool CompileOptionsStrict
    {
        get => _strict;
        set => _strict = value;
    }

    /// <summary>
    /// Defaults to <see cref="DefaultNoFallthroughCasesInSwitch"/>.
    /// </summary>
    public bool CompileOptionsNoFallthroughCasesInSwitch
    {
        get => _noFallthroughCasesInSwitch;
        set => _noFallthroughCasesInSwitch = value;
    }

    /// <summary>
    /// Defaults to <see cref="DefaultNoImplicitOverride"/>.
    /// </summary>
    public bool CompileOptionsNoImplicitOverride
    {
        get => _noImplicitOverride;
        set => _noImplicitOverride = value;
    }

    /// <summary>
    /// Defaults to <see cref="DefaultNoImplicitReturns"/>.
    /// </summary>
    public bool CompileOptionsNoImplicitReturns
    {
        get => _noImplicitReturns;
        set => _noImplicitReturns = value;
    }

    /// <summary>
    /// Defaults to <see cref="DefaultNoPropertyAccessFromIndexSignature"/>.
    /// </summary>
    public bool CompileOptionsNoPropertyAccessFromIndexSignature
    {
        get => _noPropertyAccessFromIndexSignature;
        set => _noPropertyAccessFromIndexSignature = value;
    }

    /// <summary>
    /// Defaults to <see cref="DefaultSkipLibCheck"/>.
    /// </summary>
    public bool CompileOptionsSkipLibCheck
    {
        get => _skipLibCheck;
        set => _skipLibCheck = value;
    }

    /// <summary>
    /// Defaults to <see cref="DefaultAllowSyntheticDefaultImports"/>.
    /// </summary>
    public bool CompileOptionsAllowSyntheticDefaultImports
    {
        get => _allowSyntheticDefaultImports;
        set => _allowSyntheticDefaultImports = value;
    }

    /// <summary>
    /// Defaults to <see cref="DefaultDeclaration"/>.
    /// </summary>
    public bool CompileOptionsDeclaration
    {
        get => _declaration;
        set => _declaration = value;
    }

    /// <summary>
    /// Defaults to <see cref="DefaultDeclarationMap"/>.
    /// </summary>
    public bool CompileOptionsDeclarationMap
    {
        get => _declarationMap;
        set => _declarationMap = value;
    }

    /// <summary>
    /// Defaults to <see cref="DefaultESModuleInterop"/>.
    /// </summary>
    public bool CompileOptionsESModuleInterop
    {
        get => _esModuleInterop;
        set => _esModuleInterop = value;
    }

    /// <summary>
    /// Defaults to <see cref="DefaultSourceMap"/>.
    /// </summary>
    public bool CompileOptionsSourceMap
    {
        get => _sourceMap;
        set => _sourceMap = value;
    }

    /// <summary>
    /// Defaults to <see cref="DefaultTarget"/>.
    /// </summary>
    public string CompileOptionsTarget
    {
        get => _target;
        set
        {
            Throw.CheckNotNullOrWhiteSpaceArgument( value );
            _target = value;
        }
    }

    /// <summary>
    /// Defaults to <see cref="DefaultModule"/>.
    /// </summary>
    public string CompileOptionsModule
    {
        get => _module;
        set
        {
            Throw.CheckNotNullOrWhiteSpaceArgument( value );
            _module = value;
        }
    }

    void UpdateSimpleCompilerOptions()
    {
        _compilerOptions["strict"] = _strict;
        _compilerOptions["noFallthroughCasesInSwitch"] = _noFallthroughCasesInSwitch;
        _compilerOptions["noImplicitOverride"] = _noImplicitOverride;
        _compilerOptions["noImplicitReturns"] = _noImplicitReturns;
        _compilerOptions["noPropertyAccessFromIndexSignature"] = _noPropertyAccessFromIndexSignature;

        _compilerOptions["skipLibCheck"] = _skipLibCheck;
        _compilerOptions["allowSyntheticDefaultImports"] = _allowSyntheticDefaultImports;
        _compilerOptions["declaration"] = _declaration;
        _compilerOptions["declarationMap"] = _declarationMap;
        _compilerOptions["esModuleInterop"] = _esModuleInterop;
        _compilerOptions["sourceMap"] = _sourceMap;

        _compilerOptions["target"] = _target;
        _compilerOptions["module"] = _module;

        _compilerOptions["resolveJsonModule"] = _resolveJsonModule;
    }


    /// <summary>
    /// Updates the inner <see cref="JsonFile.Root"/>.
    /// </summary>
    public void UpdateFileRoot()
    {
        UpdateSimpleCompilerOptions();
        _file.SetString( _compilerOptions, "baseUrl", _baseUrl.IsEmptyPath ? null : _baseUrl.Path );
        SetPaths( _compilerOptions, _coPaths );
        _file.SetStringList( _compilerOptions, "types", _coTypes );
    }

    /// <summary>
    /// Calls <see cref="UpdateFileRoot"/> and saves the <see cref="FilePath"/> file.
    /// </summary>
    public void Save()
    {
        using var f = File.Create( _file.FilePath );
        Write( f );
    }

    /// <summary>
    /// Calls <see cref="UpdateFileRoot"/> and writes this package.json to the output.
    /// </summary>
    /// <param name="output">Target outpout.</param>
    public void Write( Stream output )
    {
        UpdateFileRoot();
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
    public string WriteAsString()
    {
        UpdateFileRoot();
        var a = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter( a, new JsonWriterOptions { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping } );
        _file.Root.WriteTo( writer );
        writer.Flush();
        return Encoding.UTF8.GetString( a.WrittenSpan );
    }

    static bool ReadPaths( JsonFile f, JsonObject compilerOptions, IActivityMonitor monitor, out Dictionary<string, HashSet<string>>? result )
    {
        result = null;
        if( !f.GetNonJsonNull( compilerOptions, monitor, "paths", out JsonObject? paths ) )
        {
            return false;
        }
        if( paths != null )
        {
            result = new Dictionary<string, HashSet<string>>();
            foreach( var (name, array) in paths )
            {
                if( array == null ) continue;
                if( array is JsonArray c )
                {
                    HashSet<string> content = new HashSet<string>();
                    foreach( var item in c )
                    {
                        if( item == null ) continue;
                        if( item is not JsonValue v || !v.TryGetValue( out string? mapping ) )
                        {
                            monitor.Error( $"Unable to read \"{item.GetPath()}\" as a string." );
                            return false;
                        }
                        content.Add( mapping );
                    }
                    result.Add( name, content );
                }
                else
                {
                    monitor.Error( $"Unable to read \"{array.GetPath()}\" as an array." );
                    return false;
                }
            }
        }
        return true;
    }

    static void SetPaths( JsonObject compilerOptions, Dictionary<string, HashSet<string>>? paths )
    {
        if( paths == null )
        {
            compilerOptions.Remove( "paths" );
        }
        else
        {
            var newOne = new JsonObject();
            foreach( var (name, content) in paths )
            {
                newOne[name] = new JsonArray( content.Select( s => JsonValue.Create( s ) ).ToArray() );
            }
            compilerOptions["paths"] = newOne;
        }
    }


}
