using CK.Core;
using CK.Setup;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CK.Testing;

public static partial class TSTestHelperExtensions
{
    /// <summary>
    /// Disposable runner created by <see cref="CreateTypeScriptRunner(IMonitorTestHelper, NormalizedPath, string?, Dictionary{string, string}?, string)"/>.
    /// </summary>
    public sealed class Runner : IAsyncDisposable
    {
        readonly IMonitorTestHelper _helper;
        readonly NormalizedPath _targetProjectPath;
        readonly Dictionary<string, string>? _environmentVariables;
        readonly string _yarnCommand;
        readonly Action<IActivityMonitor>? _jestDispose;
        bool _isDisposed;
        List<object>? _onDisposeList;

        internal Runner( IMonitorTestHelper helper,
                         NormalizedPath targetProjectPath,
                         Dictionary<string, string>? environmentVariables,
                         string yarnCommand,
                         Action<IActivityMonitor>? jestDispose )
        {
            _helper = helper;
            _targetProjectPath = targetProjectPath;
            _jestDispose = jestDispose;
            _environmentVariables = environmentVariables;
            _yarnCommand = yarnCommand;
        }

        /// <summary>
        /// Runs the yarn command and fails on error.
        /// </summary>
        /// <param name="yarnCommand">
        /// When not null, specifies another yarn command that the default one specified by <see cref="CreateTypeScriptRunner"/>
        /// (that defaults to "test").
        /// </param>
        public void Run( string? yarnCommand = null )
        {
            yarnCommand ??= _yarnCommand;
            YarnHelper.RunYarn( _helper.Monitor, _targetProjectPath, yarnCommand, _environmentVariables )
                .ShouldBeTrue( $"'yarn {yarnCommand}' should be sucessfull." );
        }

        /// <summary>
        /// Build the Angular application (by calling <see cref="Run(string?)">Run( "build" )</see>) before running
        /// the command.
        /// <para>
        /// This takes a long time but unfortunately, it's not because the "*.spec.ts" succeed that
        /// everything is fine: even "src/app.component.ts" may not compile...
        /// </para>
        /// </summary>
        /// <param name="yarnCommand">
        /// When not null, specifies another yarn command that the default one specified by <see cref="CreateTypeScriptRunner"/>
        /// (that defaults to "test").
        /// </param>
        public void BuildAndRun( string? yarnCommand = null )
        {
            Run( "build" );
            Run( yarnCommand );
        }

        /// <summary>
        /// Adds cleanup function that will be called by <see cref="DisposeAsync"/>.
        /// </summary>
        /// <param name="onDispose">An asynchronous cleanup function.</param>
        public void OnDispose( Func<Task> onDispose )
        {
            Throw.CheckNotNullArgument( onDispose );
            _onDisposeList ??= new List<object>();
            _onDisposeList.Add( onDispose );
        }

        /// <summary>
        /// Adds cleanup function that will be called by <see cref="DisposeAsync"/>.
        /// </summary>
        /// <param name="onDispose">An asynchronous cleanup function.</param>
        public void OnDispose( Func<ValueTask> onDispose )
        {
            Throw.CheckNotNullArgument( onDispose );
            _onDisposeList ??= new List<object>();
            _onDisposeList.Add( onDispose );
        }

        /// <summary>
        /// Adds cleanup function that will be called by <see cref="DisposeAsync"/>.
        /// </summary>
        /// <param name="onDispose">A synchronous cleanup function.</param>
        public void OnDispose( Action onDispose )
        {
            Throw.CheckNotNullArgument( onDispose );
            _onDisposeList ??= new List<object>();
            _onDisposeList.Add( onDispose );
        }

        /// <summary>
        /// Must be called (using should be used) to revert any test setup side-effects that
        /// may have been done on the environment.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if( !_isDisposed )
            {
                _isDisposed = true;
                _jestDispose?.Invoke( _helper.Monitor );
                if( _onDisposeList != null )
                {
                    foreach( var o in _onDisposeList )
                    {
                        if( o is Func<Task> aT ) await aT();
                        if( o is Func<ValueTask> aV ) await aV();
                        else ((Action)o).Invoke();
                    }
                }
            }
        }
    }

}
