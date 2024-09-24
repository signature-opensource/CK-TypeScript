using CK.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CK.Setup;

internal static class JsonNodeExtensions
{
    /// <summary>
    /// Tries to read a json file. The file must contain an object {} (null, values or arrays are forbidden).
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="filePath">The file path. Must be <see cref="Path.IsPathFullyQualified(string)"/>.</param>
    /// <param name="mustExist">
    /// True to emit an error and return null if the file doesn't exist.
    /// By default, an empty PackageJsonFile is returned if there's no file.
    /// </param>
    /// <returns>The <see cref="PackageJsonFile"/> or null on error.</returns>
    public static JsonObject? ReadObjectFile( IActivityMonitor monitor, string filePath, bool mustExist = false )
    {
        Throw.CheckArgument( filePath is not null && Path.IsPathFullyQualified( filePath ) );
        if( File.Exists( filePath ) )
        {
            try
            {
                using var f = File.OpenRead( filePath );
                var documentOptions = new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };
                var packageContent = JsonNode.Parse( f, nodeOptions: default, documentOptions );
                Throw.CheckData( packageContent != null );
                return packageContent.AsObject();
            }
            catch( Exception ex )
            {
                monitor.Error( $"While reading '{filePath}'.", ex );
                return null;
            }
        }
        if( mustExist )
        {
            monitor.Error( $"Missing file '{filePath}'." );
            return null;
        }
        return new JsonObject();
    }

}
