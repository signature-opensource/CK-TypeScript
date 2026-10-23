using CK.Core;
using System;
using System.Globalization;
using System.IO;

namespace CK.TS.Angular.Engine;

/// <summary>
/// The untracked file written at the app root before the first write of an Angular upgrade and deleted once the
/// compile check has run, or by the undo. Found at the start of a run, it means that a previous upgrade was not verified.
/// </summary>
/// <param name="FromMajor">The Angular major of the app before the upgrade.</param>
/// <param name="StartedUtc">The UTC start time of the upgrade. It is written to the second.</param>
internal sealed record AngularUpgradeMarker( int FromMajor, DateTime StartedUtc )
{
    /// <summary>
    /// The name of the marker file at the app root.
    /// </summary>
    internal const string FileName = ".ck-angular-upgrade";

    const string _fromMajorKey = "fromMajor=";
    const string _startedKey = "started=";
    const string _startedFormat = "yyyy-MM-ddTHH:mm:ssZ";

    /// <summary>
    /// Gets whether the marker file exists, readable or not.
    /// </summary>
    /// <param name="appFolder">The app folder.</param>
    /// <returns>True when the marker file exists.</returns>
    internal static bool Exists( NormalizedPath appFolder ) => File.Exists( appFolder.AppendPart( FileName ) );

    /// <summary>
    /// Reads the marker.
    /// </summary>
    /// <param name="appFolder">The app folder.</param>
    /// <returns>The marker, or null when the file is missing or cannot be read.</returns>
    internal static AngularUpgradeMarker? Read( NormalizedPath appFolder )
    {
        string[] lines;
        try
        {
            lines = File.ReadAllLines( appFolder.AppendPart( FileName ) );
        }
        catch( IOException )
        {
            return null;
        }
        catch( UnauthorizedAccessException )
        {
            return null;
        }
        int? fromMajor = null;
        DateTime? started = null;
        foreach( var line in lines )
        {
            if( line.StartsWith( _fromMajorKey, StringComparison.Ordinal )
                && int.TryParse( line.AsSpan( _fromMajorKey.Length ), NumberStyles.None, CultureInfo.InvariantCulture, out var major ) )
            {
                fromMajor = major;
            }
            else if( line.StartsWith( _startedKey, StringComparison.Ordinal )
                     && DateTime.TryParseExact( line.AsSpan( _startedKey.Length ),
                                                _startedFormat,
                                                CultureInfo.InvariantCulture,
                                                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                                                out var time ) )
            {
                started = time;
            }
        }
        return fromMajor.HasValue && started.HasValue ? new AngularUpgradeMarker( fromMajor.Value, started.Value ) : null;
    }

    /// <summary>
    /// Writes the marker file, replacing any existing one.
    /// </summary>
    /// <param name="appFolder">The app folder.</param>
    internal void Write( NormalizedPath appFolder )
    {
        File.WriteAllText( appFolder.AppendPart( FileName ),
                           $"{_fromMajorKey}{FromMajor.ToString( CultureInfo.InvariantCulture )}\n{_startedKey}{StartedUtc.ToString( _startedFormat, CultureInfo.InvariantCulture )}\n" );
    }

    /// <summary>
    /// Deletes the marker file. Does nothing when it does not exist.
    /// </summary>
    /// <param name="appFolder">The app folder.</param>
    internal static void Delete( NormalizedPath appFolder ) => File.Delete( appFolder.AppendPart( FileName ) );
}
