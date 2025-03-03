using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.IO;

namespace CK.Transform.Space;

sealed class ApplyChangesContext
{
    readonly IActivityMonitor _monitor;
    readonly TransformerHost _host;
    readonly int _version;

    int _errorCount;
    List<TransformableSource>? _deleted;

    internal ApplyChangesContext( IActivityMonitor monitor, int version, TransformerHost host )
    {
        _monitor = monitor;
        _host = host;
        _version = version;
    }

    public IActivityMonitor Monitor => _monitor;

    public TransformerHost Host => _host;

    public int Version => _version;

    public bool LocalFileExists( TransformableSource source )
    {
        Throw.DebugAssert( source.LocalFilePath != null );
        if( !File.Exists( source.LocalFilePath ) )
        {
            _deleted ??= new List<TransformableSource>();
            _deleted.Add( source );
            return false;
        }
        return true;
    }

    public bool HasError => _errorCount > 0;

    public void AddError( string error, Exception? ex = null )
    {
        Throw.DebugAssert( !string.IsNullOrWhiteSpace( error ) );
        _monitor.Error( error, ex );
        ++_errorCount;
    }

}
