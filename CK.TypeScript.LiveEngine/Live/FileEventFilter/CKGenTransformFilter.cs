using CK.TypeScript.LiveEngine;
using System;
using System.IO;

sealed class CKGenTransformFilter : IFileEventFilter
{
    readonly LiveStatePathContext _pathContext;

    public static object PrimaryStateFileChanged => typeof(CKGenTransformFilter);

    public CKGenTransformFilter( LiveStatePathContext pathContext )
    {
        _pathContext = pathContext;
    }

    public object? GetChange( string path )
    {
        if( path.Length > _pathContext.CKGenTransformPath.Length
            && path[_pathContext.CKGenTransformPath.Length-1] == Path.DirectorySeparatorChar )
        {
            if( path == _pathContext.PrimaryStateFile )
            {
                return PrimaryStateFileChanged;
            }
            if( path.StartsWith( _pathContext.CKGenTransformPath, StringComparison.Ordinal ) )
            {
                return new ChangedEvent( null, path.Substring( _pathContext.CKGenTransformPath.Length ) );
            }
        }
        return null;
    }
}
