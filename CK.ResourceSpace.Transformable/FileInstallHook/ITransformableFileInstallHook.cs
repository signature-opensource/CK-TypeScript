using CK.BinarySerialization;
using System.Collections.Immutable;

namespace CK.Core;

/// <summary>
/// Basic low-level interface. Designed as potentially stateless but instances
/// must be serializable (typically by implementing <see cref="ICKSimpleBinarySerializable"/> support).
/// </summary>
public interface ITransformableFileInstallHook
{
    /// <summary>
    /// Install action that encapsulates the next hooks. 
    /// </summary>
    /// <param name="monitor">Monitor to use.</param>
    /// <param name="item">The item to install.</param>
    /// <param name="finalText">Final text, potentially transformed.</param>
    public delegate void NextHook( IActivityMonitor monitor, IInstallableItem item, string finalText );

    /// <summary>
    /// Must handle the item to install. This has full control on the installation (including absolutely doing
    /// nothing by not calling <paramref name="next"/>).
    /// </summary>
    /// <param name="monitor">Monitor to use.</param>
    /// <param name="item">The item to install.</param>
    /// <param name="finalText">Final text, potentially transformed.</param>
    /// <param name="installer">Final installer to use.</param>
    /// <param name="next">Next hook to call by default.</param>
    void Install( IActivityMonitor monitor,
                  IInstallableItem item,
                  string finalText,
                  IResourceSpaceItemInstaller installer,
                  NextHook next );


    /// <summary>
    /// Builds the final <see cref="NextHook"/> from hooks.
    /// </summary>
    /// <param name="hooks">The hooks.</param>
    /// <param name="installer">The final installer.</param>
    /// <returns>The final install action.</returns>
    internal static NextHook BuildInstallAction( ImmutableArray<ITransformableFileInstallHook> hooks,
                                                      IResourceSpaceItemInstaller installer )
    {
        NextHook last = ( monitor, item, text ) => installer.Write( item.TargetPath, text );
        for( int i = hooks.Length - 1; i >= 0; i-- )
        {
            var h = hooks[i];
            last = ( monitor, item, text ) => h.Install( monitor, item, text, installer, last );
        }
        return last;
    }
}
