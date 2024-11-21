using CK.TypeScript;
using System;
using System.Runtime.CompilerServices;

namespace CK.TS.Angular;

/// <summary>
/// Required attribute for <see cref="NgRoutedComponent"/>.
/// </summary>
public class NgRoutedComponentAttribute<T> : NgRoutedComponentAttribute where T : TypeScriptPackage
{
    /// <summary>
    /// Initializes a new <see cref="NgRoutedComponentAttribute"/>.
    /// </summary>
    /// <param name="targetRoutedComponent">The routed component under which this component must appear.</param>
    /// <param name="callerFilePath">Automatically set by the Roslyn compiler and used to compute the associated embedded resource folder.</param>
    public NgRoutedComponentAttribute( Type targetRoutedComponent,
                                       [CallerFilePath] string? callerFilePath = null )
        : base( targetRoutedComponent, callerFilePath )
    {
    }

    /// <inheritdoc />
    public override Type? Package => typeof( T );
}
