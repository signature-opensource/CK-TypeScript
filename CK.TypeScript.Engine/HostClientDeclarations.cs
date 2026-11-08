using CK.Core;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CK.Setup;

/// <summary>
/// A temporary help, waiting for the Unified Configuration: a client application of the BinPath must be declared by the
/// party that uses it ("CK-AppIdentity:Local:Client:{Name}", see CK.AppIdentity.Client). When the BinPath's project is a
/// host (its folder name ends with ".Host") that has an "appsettings.json", the missing declarations of the root party
/// are added to it.
/// <para>
/// It only adds: an existing declaration is never changed nor removed. The comments are kept (as block comments); the
/// layout is re-indented.
/// </para>
/// </summary>
static class HostClientDeclarations
{
    const string AppIdentitySection = "CK-AppIdentity";
    const string LocalSection = "Local";
    const string ClientSection = "Client";

    /// <summary>
    /// Adds the missing declarations to the "appsettings.json" of the host project.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="projectPath">The BinPath's project path (the host project folder).</param>
    /// <param name="clientNames">The client applications of the BinPath.</param>
    /// <returns>False on error (the file can't be read or written).</returns>
    public static bool EnsureDeclared( IActivityMonitor monitor, NormalizedPath projectPath, IReadOnlyCollection<string> clientNames )
    {
        if( clientNames.Count == 0
            || projectPath.IsEmptyPath
            || !projectPath.LastPart.EndsWith( ".Host", StringComparison.OrdinalIgnoreCase ) )
        {
            return true;
        }
        var file = projectPath.AppendPart( "appsettings.json" );
        if( !File.Exists( file ) ) return true;
        try
        {
            var bytes = File.ReadAllBytes( file );
            var result = AddMissing( bytes, clientNames, out var added );
            if( result == null ) return true;
            File.WriteAllBytes( file, result );
            monitor.Info( $"""
                Added "{AppIdentitySection}:{LocalSection}:{ClientSection}:{added.Concatenate( $"\", \"{AppIdentitySection}:{LocalSection}:{ClientSection}:" )}" to '{file}': the root party declares its client applications.
                This is a temporary help waiting for the Unified Configuration: commit this change.
                """ );
            return true;
        }
        catch( Exception ex )
        {
            monitor.Error( $"While declaring the client applications in '{file}'.", ex );
            return false;
        }
    }

    /// <summary>
    /// Returns the new content of an "appsettings.json" with the missing declarations, or null when nothing is missing.
    /// </summary>
    /// <param name="content">The current content (UTF-8, with or without BOM).</param>
    /// <param name="clientNames">The names of the client applications.</param>
    /// <param name="added">The names that have been added.</param>
    /// <returns>The new content or null.</returns>
    internal static byte[]? AddMissing( ReadOnlySpan<byte> content, IReadOnlyCollection<string> clientNames, out List<string> added )
    {
        ReadOnlySpan<byte> bom = [0xEF, 0xBB, 0xBF];
        bool hasBom = content.StartsWith( bom );
        if( hasBom ) content = content.Slice( 3 );

        var readerOptions = new JsonReaderOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
        // The configuration keys are case insensitive.
        using( var doc = JsonDocument.Parse( content.ToArray(), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true } ) )
        {
            var root = doc.RootElement;
            Throw.CheckData( "The appsettings.json must be a JSON object.", root.ValueKind == JsonValueKind.Object );
            // A section of the path that is not an object can't receive the declarations: the configuration is invalid.
            bool valid = IsObjectOrMissing( root, AppIdentitySection, out var a );
            if( valid && a.ValueKind == JsonValueKind.Object )
            {
                valid = IsObjectOrMissing( a, LocalSection, out var l );
                if( valid && l.ValueKind == JsonValueKind.Object ) valid = IsObjectOrMissing( l, ClientSection, out _ );
            }
            Throw.CheckData( $"\"{AppIdentitySection}\", its \"{LocalSection}\" and its \"{ClientSection}\" must be JSON objects.", valid );
            var declared = new HashSet<string>( StringComparer.OrdinalIgnoreCase );
            if( TryGetObject( root, AppIdentitySection, out var ckApp )
                && TryGetObject( ckApp, LocalSection, out var local )
                && TryGetObject( local, ClientSection, out var clients ) )
            {
                foreach( var p in clients.EnumerateObject() ) declared.Add( p.Name );
            }
            added = clientNames.Where( n => !declared.Contains( n ) ).ToList();
        }
        if( added.Count == 0 ) return null;

        var output = new ArrayBufferWriter<byte>();
        var newLine = content.IndexOf( "\r\n"u8 ) >= 0 ? "\r\n" : "\n";
        using( var w = new Utf8JsonWriter( output, new JsonWriterOptions { Indented = true, NewLine = newLine, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping } ) )
        {
            var r = new Utf8JsonReader( content, readerOptions with { CommentHandling = JsonCommentHandling.Allow } );
            // The property names of the opened objects: the path of the current object.
            var path = new List<string?>();
            string? pendingName = null;
            // What exists along the path to the "Client" section.
            bool hasAppIdentity = false, hasLocal = false, hasClient = false;
            while( r.Read() )
            {
                switch( r.TokenType )
                {
                    case JsonTokenType.PropertyName:
                        pendingName = r.GetString();
                        w.WritePropertyName( pendingName! );
                        continue;
                    case JsonTokenType.StartObject:
                        if( IsPath( path, pendingName, 0 ) && Eq( pendingName, AppIdentitySection ) ) hasAppIdentity = true;
                        else if( IsPath( path, pendingName, 1 ) && Eq( pendingName, LocalSection ) ) hasLocal = true;
                        else if( IsPath( path, pendingName, 2 ) && Eq( pendingName, ClientSection ) ) hasClient = true;
                        path.Add( pendingName );
                        w.WriteStartObject();
                        break;
                    case JsonTokenType.EndObject:
                        // Before closing an object of the path to the "Client" section, adds what is missing in it.
                        int depth = path.Count - 1;
                        if( depth == 0 && !hasAppIdentity ) WriteSections( w, 0, added );
                        else if( depth == 1 && Eq( path[1], AppIdentitySection ) && !hasLocal ) WriteSections( w, 1, added );
                        else if( depth == 2 && Eq( path[1], AppIdentitySection ) && Eq( path[2], LocalSection ) && !hasClient ) WriteSections( w, 2, added );
                        else if( depth == 3 && Eq( path[1], AppIdentitySection ) && Eq( path[2], LocalSection ) && Eq( path[3], ClientSection ) ) WriteSections( w, 3, added );
                        path.RemoveAt( depth );
                        w.WriteEndObject();
                        break;
                    case JsonTokenType.StartArray:
                        path.Add( pendingName );
                        w.WriteStartArray();
                        break;
                    case JsonTokenType.EndArray:
                        path.RemoveAt( path.Count - 1 );
                        w.WriteEndArray();
                        break;
                    case JsonTokenType.Comment:
                        w.WriteCommentValue( r.GetComment() );
                        continue;
                    case JsonTokenType.String:
                        w.WriteStringValue( r.GetString() );
                        break;
                    case JsonTokenType.Number:
                        w.WriteRawValue( r.ValueSpan, skipInputValidation: true );
                        break;
                    case JsonTokenType.True: w.WriteBooleanValue( true ); break;
                    case JsonTokenType.False: w.WriteBooleanValue( false ); break;
                    case JsonTokenType.Null: w.WriteNullValue(); break;
                }
                pendingName = null;
            }
        }
        var written = output.WrittenSpan;
        var result = new byte[(hasBom ? 3 : 0) + written.Length + Encoding.UTF8.GetByteCount( newLine )];
        if( hasBom ) bom.CopyTo( result );
        written.CopyTo( result.AsSpan( hasBom ? 3 : 0 ) );
        Encoding.UTF8.GetBytes( newLine ).CopyTo( result.AsSpan( result.Length - Encoding.UTF8.GetByteCount( newLine ) ) );
        return result;

        // The object that starts is at "depth" in the path to the "Client" section when its parents are the sections above it.
        static bool IsPath( List<string?> path, string? name, int depth )
        {
            if( path.Count != depth + 1 ) return false;
            return depth switch
            {
                0 => true,
                1 => Eq( path[1], AppIdentitySection ),
                _ => Eq( path[1], AppIdentitySection ) && Eq( path[2], LocalSection )
            };
        }

        static bool Eq( string? a, string b ) => string.Equals( a, b, StringComparison.OrdinalIgnoreCase );

        // Writes the missing sections from the given level (0: in the root object) down to the client declarations.
        static void WriteSections( Utf8JsonWriter w, int level, List<string> names )
        {
            string[] sections = [AppIdentitySection, LocalSection, ClientSection];
            for( int i = level; i < sections.Length; ++i )
            {
                w.WritePropertyName( sections[i] );
                w.WriteStartObject();
            }
            foreach( var n in names )
            {
                w.WritePropertyName( n );
                w.WriteStartObject();
                w.WriteEndObject();
            }
            for( int i = level; i < sections.Length; ++i ) w.WriteEndObject();
        }
    }

    static bool IsObjectOrMissing( JsonElement e, string name, out JsonElement found )
    {
        foreach( var p in e.EnumerateObject() )
        {
            if( string.Equals( p.Name, name, StringComparison.OrdinalIgnoreCase ) )
            {
                found = p.Value;
                return p.Value.ValueKind == JsonValueKind.Object;
            }
        }
        found = default;
        return true;
    }

    static bool TryGetObject( JsonElement e, string name, out JsonElement found )
    {
        foreach( var p in e.EnumerateObject() )
        {
            if( string.Equals( p.Name, name, StringComparison.OrdinalIgnoreCase ) && p.Value.ValueKind == JsonValueKind.Object )
            {
                found = p.Value;
                return true;
            }
        }
        found = default;
        return false;
    }
}
