using CK.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace CK.Setup;

/// <summary>
/// A temporary help, waiting for the Unified Configuration: a client application of the BinPath must be declared by the
/// party that uses it ("CK-AppIdentity:Local:Client:{Name}", see CK.AppIdentity.Client). When the BinPath's project is a
/// host (its folder name ends with ".Host") that has an "appsettings.json", the missing declarations of the root party
/// are added to it.
/// <para>
/// It only adds: an existing declaration is never changed nor removed. Only the new text is inserted: the rest of the
/// file (its comments, its layout, its indentation) is kept byte for byte.
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

        // Textual insertion: only the new text is inserted, everything else is kept byte for byte (the comments, the
        // layout, the indentation). The reader finds where: before the closing brace of the deepest existing object
        // of the path root / "CK-AppIdentity" / "Local" / "Client".
        var found = new ObjectInfo?[4];
        var stack = new List<ObjectInfo>();
        string? pendingName = null;
        var r = new Utf8JsonReader( content, readerOptions );
        while( r.Read() )
        {
            var top = stack.Count > 0 ? stack[^1] : null;
            switch( r.TokenType )
            {
                case JsonTokenType.PropertyName:
                    Throw.DebugAssert( top != null );
                    if( top.FirstMember < 0 ) top.FirstMember = (int)r.TokenStartIndex;
                    pendingName = r.GetString();
                    continue;
                case JsonTokenType.StartObject:
                    var o = new ObjectInfo { Open = (int)r.TokenStartIndex };
                    if( top == null ) o.Level = 0;
                    else if( top.Level == 0 && Eq( pendingName, AppIdentitySection ) ) o.Level = 1;
                    else if( top.Level == 1 && Eq( pendingName, LocalSection ) ) o.Level = 2;
                    else if( top.Level == 2 && Eq( pendingName, ClientSection ) ) o.Level = 3;
                    stack.Add( o );
                    break;
                case JsonTokenType.StartArray:
                    // The objects of an array belong to no level.
                    stack.Add( new ObjectInfo { IsArray = true } );
                    break;
                case JsonTokenType.EndObject:
                case JsonTokenType.EndArray:
                    var closed = stack[^1];
                    stack.RemoveAt( stack.Count - 1 );
                    if( !closed.IsArray )
                    {
                        closed.Close = (int)r.TokenStartIndex;
                        if( closed.Level >= 0 && found[closed.Level] == null ) found[closed.Level] = closed;
                    }
                    if( stack.Count > 0 && !stack[^1].IsArray ) stack[^1].LastValueEnd = (int)r.BytesConsumed;
                    break;
                default:
                    // A primitive value of a member.
                    if( top is { IsArray: false } ) top.LastValueEnd = (int)r.BytesConsumed;
                    break;
            }
            pendingName = null;
        }
        var rootInfo = found[0];
        Throw.DebugAssert( rootInfo != null );
        // The deepest existing level: the missing ones are inserted in it.
        int level = found[3] != null ? 3 : found[2] != null ? 2 : found[1] != null ? 1 : 0;
        var target = found[level]!;

        var text = content.ToArray();
        var newLine = content.IndexOf( "\r\n"u8 ) >= 0 ? "\r\n" : "\n";
        string[] sections = [AppIdentitySection, LocalSection, ClientSection];
        // The new members of the target: the client declarations, or the first missing section (on one line).
        var declarations = added.Select( n => $"\"{n}\": {{}}" ).ToList();
        List<string> members;
        if( level == 3 ) members = declarations;
        else
        {
            var inner = $"{{ {string.Join( ", ", declarations )} }}";
            for( int i = sections.Length - 1; i >= level; --i )
            {
                inner = i == level ? $"\"{sections[i]}\": {inner}" : $"{{ \"{sections[i]}\": {inner} }}";
            }
            members = [inner];
        }
        bool hasMembers = target.FirstMember >= 0;
        bool needsComma = hasMembers && !HasTrailingComma( text, target.LastValueEnd, target.Close );
        var insertions = new List<(int At, string Text)>();
        int closeLineStart = LineStart( text, target.Close );
        if( IsBlank( text, closeLineStart, target.Close ) && LineStart( text, target.Open ) != closeLineStart )
        {
            // The closing brace is on its own line: one line per new member, before it.
            string indent = hasMembers
                            ? Indent( text, target.FirstMember )
                            : Indent( text, target.Close ) + IndentUnit( text, rootInfo );
            var b = new StringBuilder();
            for( int i = 0; i < members.Count; ++i )
            {
                b.Append( indent ).Append( members[i] );
                if( i < members.Count - 1 ) b.Append( ',' );
                b.Append( newLine );
            }
            insertions.Add( (closeLineStart, b.ToString()) );
        }
        else
        {
            // A compact object ("{ ... }" on one line): the new members before its closing brace.
            var inline = string.Join( ", ", members );
            insertions.Add( (target.Close, (text[target.Close - 1] == (byte)' ' ? "" : " ") + inline + " ") );
        }
        if( needsComma ) insertions.Add( (target.LastValueEnd, ",") );

        var result = new List<byte>( text.Length + 256 );
        if( hasBom ) result.AddRange( bom.ToArray() );
        int pos = 0;
        foreach( var (at, t) in insertions.OrderBy( x => x.At ) )
        {
            result.AddRange( text.AsSpan( pos, at - pos ).ToArray() );
            result.AddRange( Encoding.UTF8.GetBytes( t ) );
            pos = at;
        }
        result.AddRange( text.AsSpan( pos ).ToArray() );
        return result.ToArray();

        static bool Eq( string? a, string b ) => string.Equals( a, b, StringComparison.OrdinalIgnoreCase );
    }

    sealed class ObjectInfo
    {
        public bool IsArray;
        public int Level = -1;
        public int Open;
        public int Close;
        public int FirstMember = -1;
        public int LastValueEnd = -1;
    }

    static int LineStart( byte[] text, int pos )
    {
        while( pos > 0 && text[pos - 1] != (byte)'\n' ) --pos;
        return pos;
    }

    static bool IsBlank( byte[] text, int from, int to )
    {
        for( int i = from; i < to; ++i ) if( text[i] != (byte)' ' && text[i] != (byte)'\t' ) return false;
        return true;
    }

    // The whitespace at the start of the line of a position.
    static string Indent( byte[] text, int pos )
    {
        int start = LineStart( text, pos );
        int end = start;
        while( end < text.Length && (text[end] == (byte)' ' || text[end] == (byte)'\t') ) ++end;
        return Encoding.UTF8.GetString( text, start, end - start );
    }

    // The indentation unit of the file: the one of the first member of the root (2 spaces by default).
    static string IndentUnit( byte[] text, ObjectInfo root )
    {
        if( root.FirstMember < 0 || LineStart( text, root.FirstMember ) == LineStart( text, root.Open ) ) return "  ";
        var unit = Indent( text, root.FirstMember );
        return unit.Length > 0 ? unit : "  ";
    }

    // Whether a comma follows the last member's value (a trailing comma, allowed by the reader): skips the whitespace
    // and the comments.
    static bool HasTrailingComma( byte[] text, int from, int to )
    {
        int i = from;
        while( i < to )
        {
            byte c = text[i];
            if( c == (byte)',' ) return true;
            if( c == (byte)'/' && i + 1 < to && text[i + 1] == (byte)'/' )
            {
                while( i < to && text[i] != (byte)'\n' ) ++i;
            }
            else if( c == (byte)'/' && i + 1 < to && text[i + 1] == (byte)'*' )
            {
                i += 2;
                while( i + 1 < to && !(text[i] == (byte)'*' && text[i + 1] == (byte)'/') ) ++i;
                i += 2;
            }
            else ++i;
        }
        return false;
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
