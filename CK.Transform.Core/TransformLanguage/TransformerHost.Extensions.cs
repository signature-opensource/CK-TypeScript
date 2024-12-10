using CK.Core;

namespace CK.Transform.TransformLanguage;


public sealed partial class TransformerHost
{
    /// <summary>
    /// Easy to use transform function.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="source">The text to transform.</param>
    /// <param name="transformer">The transformer function.</param>
    /// <returns>The transformed text or null on error.</returns>
    public string? Transform( IActivityMonitor monitor, string source, string transformer )
    {
        var f = ParseFunction( transformer );
        var n = Transform( monitor, source, f );
        return n?.ToString();
    }

}
