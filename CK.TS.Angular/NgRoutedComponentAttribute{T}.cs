using System.Runtime.CompilerServices;

namespace CK.TS.Angular;

/// <summary>
/// Required attribute for <see cref="NgRoutedComponent"/> with a strongly
/// typed <see cref="NgRoutedComponentAttribute.TargetComponent"/>.
/// </summary>
public class NgRoutedComponentAttribute<T> : NgRoutedComponentAttribute where T : INgComponent
{
    /// <summary>
    /// Initializes a new <see cref="NgComponentAttribute"/>.
    /// </summary>
    /// <param name="callerFilePath">The caller path.</param>
    public NgRoutedComponentAttribute( [CallerFilePath] string? callerFilePath = null )
        : base( typeof( T ), callerFilePath )
    {
    }
}
