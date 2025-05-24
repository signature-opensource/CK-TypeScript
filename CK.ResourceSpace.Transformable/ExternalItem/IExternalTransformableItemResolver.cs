using CK.Transform.Core;
using System;

namespace CK.Core;

/// <summary>
/// Optional strategy for the <see cref="TransformableFileHandler"/> that can provide
/// items to transform (only during the initial setup phasis) that exist outside
/// of the resource space.
/// <para>
/// These <see cref="ExternalTransformableItem"/> provide their text and know how to
/// install a transformed text that replaces the external item.
/// </para>
/// </summary>
public interface IExternalTransformableItemResolver
{
    /// <summary>
    /// Must locate an item that must be transformed by an idempotent transformer.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="transformer">
    /// The transformer function with the <see cref="TransformerFunction.Target"/> to resolve.
    /// The target always starts with "../": this conventionally identifies a path to an external item.
    /// </param>
    /// <param name="expectedPath">The expected target path (starting with "../").</param>
    /// <param name="isNamePrefix">
    /// True if <paramref name="name"/> must be considered as a prefix, false if name is the exact item name.
    /// Whether this notion of prefix applies to the type of item is left to the implementation.
    /// </param>
    /// <param name="name">The target item name to locate.</param>
    /// <returns>The resolved item on success, null on error. Errors must be logged.</returns>
    ExternalTransformableItem? Resolve( IActivityMonitor monitor,
                                        TransformerFunction transformer,
                                        ReadOnlySpan<char> expectedPath,
                                        bool isNamePrefix,
                                        ReadOnlySpan<char> name );
}
