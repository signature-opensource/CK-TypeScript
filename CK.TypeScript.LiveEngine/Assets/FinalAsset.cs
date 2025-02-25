using CK.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace CK.TypeScript.LiveEngine;

/// <summary>
/// A final asset is a resource and a the last write time of its local file.
/// The <see cref="LastWriteTime"/> is <see cref="Util.UtcMinValue"/> for resources
/// in regular packages.
/// </summary>
/// <param name="Origin">The resource itself.</param>
/// <param name="LocalPath">The local path or null if the resource is from a regular package.</param>
/// <param name="LastWriteTime">The origin last write time.</param>
readonly record struct FinalAsset( ResourceLocator Origin, string? LocalPath, DateTime LastWriteTime )
{
    public static FinalAsset ToFinal( in ResourceLocator r )
    {
        var local = r.LocalFilePath;
        DateTime lwt = local != null ? File.GetLastWriteTimeUtc( local ) : Util.UtcMinValue;
        return new FinalAsset( r, local, lwt );
    }

    public bool Exists => Origin.LocalFilePath == null || LastWriteTime != FileUtil.MissingFileLastWriteTimeUtc;

}
