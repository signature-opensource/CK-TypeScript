namespace CK.Transform.Core;

/// <summary>
/// Captures a binding error of <see cref="SourceSpan.Bind"/>.
/// </summary>
public readonly struct BindingError
{
    readonly TokenError _error;
    readonly int _index;

    internal BindingError( TokenError error, int index )
    {
        _error = error;
        _index = index;
    }

    /// <summary>
    /// Gets the binding error description.
    /// </summary>
    public TokenError Error => _error;

    /// <summary>
    /// Gets the token index of the error.
    /// </summary>
    public int Index => _index;
}
