using CK.Core;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace CK.Setup;

/// <summary>
/// Basic wrapper around a <see cref="Root"/> JsonObject and its <see cref="FilePath"/>.
/// <para>
/// This exposes helpers that never throw but logs their errors.
/// </para>
/// </summary>
/// <param name="Root">Mutable root.</param>
/// <param name="FilePath">Rooted full path of this file.</param>
readonly record struct JsonFile( JsonObject Root, NormalizedPath FilePath )
{
    /// <summary>
    /// Tries to read a json file. The file must contain an object {} (null, values or arrays are forbidden).
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="filePath">The file path. Must be <see cref="NormalizedPath.IsRooted"/>.</param>
    /// <param name="mustExist">
    /// True to emit an error and return null if the file doesn't exist.
    /// By default, an empty PackageJsonFile is returned if there's no file.
    /// </param>
    /// <returns>The <see cref="JsonFile"/> or null on error.</returns>
    public static JsonFile? ReadFile( IActivityMonitor monitor, NormalizedPath filePath, bool mustExist = false )
    {
        Throw.CheckArgument( filePath.Parts.Count >= 2 && filePath.IsRooted );
        var root = JsonNodeExtensions.ReadObjectFile( monitor, filePath, mustExist );
        return root != null ? new JsonFile( root, filePath ) : default;
    }

    /// <summary>
    /// Reads a dictionary from a Json object property that is missing or must be a Json object.
    /// Empty or whitespace keys are ignored by default.
    /// </summary>
    /// <param name="o">This object.</param>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="propertyName">The property to read.</param>
    /// <param name="result">The dictionary or null if it is missing.</param>
    /// <param name="skipEmptyOrWhitespaceKey">False to allow an empty or whitespace key.</param>
    /// <returns>True on success, false on error.</returns>
    public bool ReadStringDictionary( JsonObject o,
                                      IActivityMonitor monitor,
                                      string propertyName,
                                      out Dictionary<string, string>? result,
                                      bool skipEmptyOrWhitespaceKey = true )
    {
        result = null;
        if( !GetNonJsonNull<JsonObject>( o, monitor, propertyName, out var section ) )
        {
            return false;
        }
        if( section != null )
        {
            result = new Dictionary<string, string>();
            foreach( var (name, content) in section )
            {
                if( (!skipEmptyOrWhitespaceKey || !string.IsNullOrWhiteSpace( name ))
                    && content is JsonValue c
                    && c.TryGetValue( out string? s ) )
                {
                    result.Add( name, s );
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Sets the whole "<paramref name="propertyName"/>" property content or removes it.
    /// </summary>
    /// <param name="o">The parent object.</param>
    /// <param name="propertyName">The property to read.</param>
    /// <param name="dictionary">The property names and their values. Null to remove it.</param>
    public void SetStringDictionary( JsonObject o, string propertyName, IEnumerable<KeyValuePair<string, string>>? dictionary )
    {
        Throw.CheckArgument( o is not null && o.Root == Root );
        Throw.CheckNotNullArgument( propertyName );
        if( dictionary == null )
        {
            o.Remove( propertyName );
        }
        else
        {
            var newOne = new JsonObject();
            foreach( var (name, content) in dictionary )
            {
                newOne[name] = content;
            }
            o[propertyName] = newOne;
        }
    }

    /// <summary>
    /// Reads a list of non nullable strings from a Json object property that is missing or must be an array.
    /// </summary>
    /// <param name="o">The parent object.</param>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="propertyName">The property to read.</param>
    /// <param name="result">The list of strings or null if it is missing.</param>
    /// <returns>True on success, false on error.</returns>
    public bool ReadStringList( JsonObject o, IActivityMonitor monitor, string propertyName, out List<string>? result )
    {
        result = null;
        if( !GetNonJsonNull<JsonArray>( o, monitor, propertyName, out var array ) )
        {
            return false;
        }
        if( array != null )
        {
            result = new List<string>();
            bool success = true;
            int i = 0;
            foreach( var node in array )
            {
                if( node is JsonValue v && v.TryGetValue( out string? s ) )
                {
                    result.Add( s );
                }
                else
                {
                    monitor.Error( $"Array value \"{o.GetPath()}[{i}]\" is not a string." );
                }
                ++i;
            }
            return success;
        }
        return true;
    }

    /// <summary>
    /// Sets an array of string or removes it.
    /// </summary>
    /// <param name="o">The parent object.</param>
    /// <param name="propertyName">The property to read.</param>
    /// <param name="strings">The strings.</param>
    public void SetStringList( JsonObject o, string propertyName, IEnumerable<string>? strings )
    {
        Throw.CheckArgument( o is not null && o.Root == Root );
        Throw.CheckNotNullArgument( propertyName );
        if( strings == null )
        {
            o.Remove( propertyName );
        }
        else
        {
            var newOne = new JsonArray();
            foreach( var s in strings )
            {
                newOne.Add( JsonValue.Create( s ) );
            }
            o[propertyName] = newOne;
        }
    }

    /// <summary>
    /// Fix the JsonNode API that don't handle "null" (no easy way to differenciate Json token "null" from a missing property).
    /// This method considers a "null" token as being not the expected <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Either JsonValue, JsonArray or JsonObject.</typeparam>
    /// <param name="o">The parent object.</param>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="propertyName">The property name to consider.</param>
    /// <param name="result">The typed "property" object. Null if the property doesn't exist.</param>
    /// <returns>True on success (the <paramref name="propertyName"/> may not exist), or false if the property was "null" or not of the right type.</returns>
    public bool GetNonJsonNull<T>( JsonObject o, IActivityMonitor monitor, string propertyName, out T? result ) where T : JsonNode
    {
        Throw.CheckArgument( o is not null && o.Root == Root );
        Throw.CheckNotNullArgument( propertyName );
        result = default;
        if( !o.TryGetPropertyValue( propertyName, out var n ) ) return true;
        if( n is not T sub )
        {
            monitor.Error( $"Json property \"{o.GetPath()}.{propertyName}\" is not a {typeof( T ).Name}." );
            return false;
        }
        result = sub;
        return true;
    }

    /// <summary>
    /// Tries to read a string that must not be null but may be missing.
    /// </summary>
    /// <param name="o">The parent object.</param>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="propertyName">The property name to consider.</param>
    /// <param name="result">The property value. Null if the property doesn't exist.</param>
    /// <returns>True on success (the <paramref name="propertyName"/> may not exist), or false if the property was "null" or not a string.</returns>
    public bool GetNonNullJsonString( JsonObject o, IActivityMonitor monitor, string propertyName, out string? result )
    {
        result = null;
        if( !GetNonJsonNull( o, monitor, propertyName, out JsonValue? jv ) ) return false;
        if( jv != null && !jv.TryGetValue( out result ) )
        {
            monitor.Error( $"Unable to read \"{o.GetPath()}.{propertyName}\" as a string." );
            return false;
        }
        return true;
    }

    /// <summary>
    /// Tries to read a number that must not be null but may be missing.
    /// </summary>
    /// <param name="o">The parent object.</param>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="propertyName">The property name to consider.</param>
    /// <param name="result">The property value. Null if the property doesn't exist.</param>
    /// <returns>True on success (the <paramref name="propertyName"/> may not exist), or false if the property was "null" or not a number.</returns>
    public bool GetNonNullJsonNumber( JsonObject o, IActivityMonitor monitor, string propertyName, out double? result )
    {
        result = null;
        if( !GetNonJsonNull( o, monitor, propertyName, out JsonValue? jv ) ) return false;
        if( jv != null && !jv.TryGetValue( out result ) )
        {
            monitor.Error( $"Unable to read \"{o.GetPath()}.{propertyName}\" as a number (double)." );
            return false;
        }
        return true;
    }

    /// <summary>
    /// Tries to read a boolean property that must not be null but may be missing.
    /// </summary>
    /// <param name="o">The parent object.</param>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="propertyName">The property name to consider.</param>
    /// <param name="result">The property value. Null if the property doesn't exist.</param>
    /// <returns>True on success (the <paramref name="propertyName"/> may not exist), or false if the property was "null" or not a string.</returns>
    public bool GetNonNullJsonBoolean( JsonObject o, IActivityMonitor monitor, string propertyName, out bool? result )
    {
        result = null;
        if( !GetNonJsonNull( o, monitor, propertyName, out JsonValue? jv ) ) return false;
        if( jv != null && !jv.TryGetValue( out result ) )
        {
            monitor.Error( $"Unable to read \"{o.GetPath()}.{propertyName}\" as a boolean." );
            return false;
        }
        return true;
    }

    /// <summary>
    /// Inserts, updates or deletes the property.
    /// </summary>
    /// <param name="o">The parent object.</param>
    /// <param name="propertyName">The property name to set.</param>
    /// <param name="value">The value. Null to remove it.</param>
    public void SetString( JsonObject o, string propertyName, string? value )
    {
        if( value == null ) o.Remove( propertyName );
        else o[propertyName] = value;
    }

    /// <summary>
    /// Inserts, updates or deletes the property.
    /// </summary>
    /// <param name="o">The parent object.</param>
    /// <param name="propertyName">The property name to set.</param>
    /// <param name="value">The value. Null to remove it.</param>
    public void SetBoolean( JsonObject o, string propertyName, bool? value )
    {
        if( value == null ) o.Remove( propertyName );
        else o[propertyName] = value.Value;
    }

    /// <summary>
    /// Inserts, updates or deletes the property.
    /// </summary>
    /// <param name="o">The parent object.</param>
    /// <param name="propertyName">The property name to set.</param>
    /// <param name="value">The value. Null to remove it.</param>
    public void SetNumber( JsonObject o, string propertyName, double? value )
    {
        if( value == null ) o.Remove( propertyName );
        else o[propertyName] = value.Value;
    }
}
