//using CK.BinarySerialization;
//using CK.EmbeddedResources;
//using CK.Transform.Core;
//using System;
//using System.Threading;

//namespace CK.Core;

///// <summary>
///// Models a source file (a resource) that can be a final <see cref="TItem"/>
///// or a <see cref="TFunctionSource"/>.
///// </summary>
//partial class TransformableSource
//{
//    readonly IResPackageResources _resources;
//    readonly ResourceLocator _origin;
//    string _text;
//    bool _isShot;

//    protected TransformableSource( IResPackageResources resources, ResourceLocator origin, string text )
//    {
//        Throw.DebugAssert( !string.IsNullOrEmpty( text ) );
//        _resources = resources;
//        _origin = origin;
//        _text = text;
//    }

//    public ResourceLocator Origin => _origin;

//    /// <summary>
//    /// Gets the text of this source.
//    /// </summary>
//    public string Text
//    {
//        get
//        {
//            Throw.DebugAssert( _text != null );
//            return _text;
//        }
//    }

//    public IResPackageResources Resources => _resources;

//    internal bool IsShot => _isShot;

//    /// <summary>
//    /// Shoots this source.
//    /// </summary>
//    public void Shoot() => _isShot = true;

//    /// <summary>
//    /// Tries to read again the resource and on success, propagate its
//    /// change in the structure.
//    /// </summary>
//    /// <param name="monitor">The monitor to track errors.</param>
//    /// <param name="transformerHost">The transformer host.</param>
//    /// <returns>True on success, false on error: this source must be removed from the environment.</returns>
//    public bool TryRevive( IActivityMonitor monitor, TransformerHost transformerHost )
//    {
//        Throw.DebugAssert( _isShot );
//        string? newText = null;
//        int retryCount = 0;
//        retry:
//        if( _origin.IsValid )
//        {
//            try
//            {
//                newText = _origin.ReadAsText();
//            }
//            catch( Exception ex )
//            {
//                if( ++retryCount < 3 )
//                {
//                    monitor.Warn( $"While reading '{_origin}'.", ex );
//                    Thread.Sleep( retryCount * 100 );
//                    goto retry;
//                }
//                monitor.Error( $"Unable to read {_origin}'.", ex );
//            }
//        }
//        if( string.IsNullOrEmpty( newText ) )
//        {
//            monitor.Info( $"Removing {_origin}." );
//            Die( monitor );
//            return false;
//        }
//        if( newText == _text )
//        {
//            monitor.Debug( $"No change for {_origin}. Skipped." );
//            return true;
//        }
//        _text = newText;
//        if( Revive( monitor, transformerHost ) )
//        {
//            _isShot = false;
//            return true;
//        }
//        return false;
//    }

//    protected virtual void Die( IActivityMonitor monitor ) { }

//    protected virtual bool Revive( IActivityMonitor monitor, TransformerHost transformerHost ) => true;

//}
