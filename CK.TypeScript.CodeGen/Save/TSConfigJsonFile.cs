using CK.Core;
using CK.TypeScript.CodeGen;
using CSemVer;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CK.Setup
{
    /// <summary>
    /// tsConfig.json file model.
    /// </summary>
    public sealed class TSConfigJsonFile
    {
        readonly JsonFile _file;
        readonly NormalizedPath _folderPath;
        readonly Dictionary<string, HashSet<string>> _paths;
        readonly JsonObject _compilerOptions;
        NormalizedPath _baseUrl;

        TSConfigJsonFile( JsonFile f,
                          JsonObject compilerOptions,
                          NormalizedPath baseUrl = default,
                          Dictionary<string, HashSet<string>>? compilerOptionsPaths = null )
        {
            _file = f;
            _baseUrl = baseUrl;
            _compilerOptions = compilerOptions;
            _paths = compilerOptionsPaths ?? new Dictionary<string, HashSet<string>>();
            _folderPath = f.FilePath.RemoveLastPart();
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
            return DoRead( monitor, nf.Value );
        }

        static TSConfigJsonFile? DoRead( IActivityMonitor monitor, JsonFile f )
        {
            bool success = f.GetNonJsonNull<JsonObject>( f.Root, monitor, "compilerOptions", out var compilerOptions );
            compilerOptions ??= new JsonObject();
            success &= f.GetNonNullJsonString( f.Root, monitor, "baseUrl", out var baseUrl );
            var paths = ReadPaths( f, compilerOptions, monitor );
            success &= paths != null;
            if( !success )
            {
                monitor.Error( $"Unable to read file '{f.FilePath}'." );
                return null;
            }
            return new TSConfigJsonFile( f, compilerOptions, baseUrl, paths );
        }

        /// <summary>
        /// Creates a new empty tsConfig.json file.
        /// </summary>
        /// <param name="filePath">The file path. Must be <see cref="NormalizedPath.IsRooted"/>.</param>
        /// <returns>An empty package.json file.</returns>
        public static TSConfigJsonFile CreateEmpty( NormalizedPath filePath )
        {
            Throw.CheckArgument( filePath.Parts.Count >= 3 && filePath.IsRooted );
            return new TSConfigJsonFile( new JsonFile( new JsonObject(), filePath ), new JsonObject() );
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
        /// Gets or sets the "baseUrl".
        /// When set to empty, it will be removed from the json when saved.
        /// </summary>
        public NormalizedPath BaseUrl { get => _baseUrl; set => _baseUrl = value; }

        /// <summary>
        /// Gets the <see cref="FilePath"/>'s directory combined with the <see cref="BaseUrl"/>.
        /// </summary>
        public NormalizedPath ResolvedBaseUrl => _folderPath.Combine( _baseUrl ).ResolveDots();

        /// <summary>
        /// Gets the "compilerOptions": { "paths": { "key", ["v1","v2"] } } paths mappings.
        /// </summary>
        public Dictionary<string, HashSet<string>> CompilerOptionsPaths => _paths;

        /// <summary>
        /// Updates the inner <see cref="JsonFile.Root"/>.
        /// </summary>
        public void UpdateFileRoot()
        {
            if( _compilerOptions.Parent == null ) _file.Root["compilerOptions"] = _compilerOptions;
            _file.SetString( _compilerOptions, "baseUrl", _baseUrl.IsEmptyPath ? null : _baseUrl.Path );
            SetPaths( _compilerOptions, _paths );
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
        public string WriteAsString()
        {
            UpdateFileRoot();
            var a = new ArrayBufferWriter<byte>();
            using var writer = new Utf8JsonWriter( a, new JsonWriterOptions { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping } );
            _file.Root.WriteTo( writer );
            writer.Flush();
            return Encoding.UTF8.GetString( a.WrittenSpan );
        }

        static Dictionary<string, HashSet<string>>? ReadPaths( JsonFile f, JsonObject compilerOptions, IActivityMonitor monitor )
        {
            if( !f.GetNonJsonNull<JsonObject>( compilerOptions, monitor, "paths", out JsonObject? paths ) )
            {
                return null;
            }
            var result = new Dictionary<string, HashSet<string>>();
            if( paths != null )
            {
                foreach( var (name, array) in paths )
                {
                    if( array == null ) continue;
                    if( array is JsonArray c )
                    {
                        HashSet<string> content = new HashSet<string>();
                        foreach( var item in c )
                        {
                            if( item == null ) continue;
                            string? mapping = null;
                            if( item is not JsonValue v || !v.TryGetValue( out mapping ) )
                            {
                                monitor.Error( $"Unable to read \"{item.GetPath()}\" as a string." );
                                return null;
                            }
                            content.Add( mapping );
                        }
                        result.Add( name, content );
                    }
                    else
                    {
                        monitor.Error( $"Unable to read \"{array.GetPath()}\" as an array." );
                        return null;
                    }
                }
            }
            return result;
        }


        static void SetPaths( JsonObject compilerOptions, Dictionary<string, HashSet<string>> paths )
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
