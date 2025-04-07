using CK.Core;
using CK.EmbeddedResources;
using System;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Bridges <see cref="ITypeScriptPublishTarget"/> on a <see cref="CodeGenResourceContainer.Builder"/>.
/// </summary>
public sealed class CodeGenResourceContainerTarget : ITypeScriptPublishTarget
{
    const string _resultDisplayName = "TypeScript code";

    IDisposableGroup? _group;
    CodeGenResourceContainer.Builder? _builder;
    IResourceContainer? _result;

    /// <summary>
    /// Gets the result. Can be a <see cref="EmptyResourceContainer"/> or a <see cref="CodeGenResourceContainer"/>.
    /// This is not null after a call to <see cref="TypeScriptRoot.Publish(IActivityMonitor, ITypeScriptPublishTarget)"/>.
    /// </summary>
    public IResourceContainer? Result => _result;

    bool ITypeScriptPublishTarget.Open( IActivityMonitor monitor, TypeScriptRoot root )
    {
        int count = root.Root.FileCount;
        if( count == 0 )
        {
            monitor.OpenWarn( $"No TypeScript files to generate. Creating an empty container for {_resultDisplayName}." );
            _result = new EmptyResourceContainer( _resultDisplayName, isDisabled: false );
        }
        else
        {
            _group = monitor.OpenInfo( $"Generating {count} TypeScript files code container." );
            _builder = new CodeGenResourceContainer.Builder( count );
        }
        return true;
    }

    void ITypeScriptPublishTarget.Add( ReadOnlySpan<char> path, string content )
    {
        Throw.CheckState( _builder != null );
        _builder.AddNext( path.ToString(), content );
    }

    bool ITypeScriptPublishTarget.Close( IActivityMonitor monitor, TypeScriptRoot root, Exception? ex )
    {
        if( _group != null )
        {
            Throw.DebugAssert( _builder != null );
            _result = _builder.Build( _resultDisplayName );
            _group.Dispose();
            _group = null;
        }
        return true;
    }

}
