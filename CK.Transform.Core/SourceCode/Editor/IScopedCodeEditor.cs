namespace CK.Transform.Core;

/// <summary>
/// Scoped editor works on the filtered <see cref="Tokens"/>.
/// </summary>
public interface IScopedCodeEditor : ICodeEditor
{
    /// <summary>
    /// Gets the filtered tokens enumerator that must be forwarded
    /// before any edit can be made.
    /// </summary>
    ITokenFilterEnumerator Tokens { get; }
}
