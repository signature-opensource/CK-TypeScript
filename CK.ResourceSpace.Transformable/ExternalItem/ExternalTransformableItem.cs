namespace CK.Core;

/// <summary>
/// External items resolved by <see cref="IExternalTransformableItemResolver"/>.
/// </summary>
public abstract class ExternalTransformableItem
{
    readonly string _initialText;
    readonly string _externalPath;

    /// <summary>
    /// Initializes a new <see cref="ExternalTransformableItem"/>.
    /// </summary>
    /// <param name="initialText">The item's text. May be empty.</param>
    /// <param name="externalPath">The external path of the item. Must not be empty or whitespace.</param>
    protected ExternalTransformableItem( string initialText, string externalPath )
    {
        Throw.CheckNotNullArgument( initialText );
        Throw.CheckNotNullOrWhiteSpaceArgument( externalPath );
        _initialText = initialText;
        _externalPath = externalPath;
    }

    /// <summary>
    /// Gets the text to transform.
    /// </summary>
    public string InitialText => _initialText;

    /// <summary>
    /// Gets the external item's path.
    /// </summary>
    public string ExternalPath => _externalPath;

    /// <summary>
    /// Updates the item with its transformed content.
    /// <para>
    /// This is not called if the <see cref="InitialText"/> has not been transformed.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="transformedText">The transformed text.</param>
    /// <returns>True on success, false on error. Errors must be logged.</returns>
    internal protected abstract bool Install( IActivityMonitor monitor, string transformedText );
}
