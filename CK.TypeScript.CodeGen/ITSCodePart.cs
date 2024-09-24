using System;
using System.Collections.Generic;
using System.Text;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Extends <see cref="ITSCodeWriter"/> to support multiple parts, optionals <see cref="Closer"/> suffix and a <see cref="Memory"/>.
/// Such parts can be created from a parent part (typically from a <see cref="TypeScriptFile.Body"/>), or as detached, independent
/// parts thanks to <see cref="TypeScriptFile.CreateDetachedPart()"/>.
/// <para>
/// A part doesn't expose the file to which it belongs and this is intended. Code generators must work with <see cref="TypeScriptFile"/>
/// and use parts locally, keeping this relationship explicit.
/// </para>
/// </summary>
public interface ITSCodePart : ITSCodeWriter
{
    /// <summary>
    /// Gets the optional string that closes this scoped part.
    /// Defaults to the empty string.
    /// </summary>
    string Closer { get; }

    /// <summary>
    /// Gets an optional memory associated to this part.
    /// </summary>
    IDictionary<object, object?> Memory { get; }

    /// <summary>
    /// Gets the created parts (keyed or not).
    /// </summary>
    public IEnumerable<ITSCodePart> Parts { get; }

    /// <summary>
    /// Creates a segment of code inside this code and returns it.
    /// </summary>
    /// <param name="closer">Optional <see cref="Closer"/> of the subordinate part.</param>
    /// <param name="top">
    /// Optionally creates the new part at the start of the code instead of at the
    /// current writing position in the code.
    /// </param>
    /// <returns>The code part to use.</returns>
    ITSCodePart CreatePart( string closer = "", bool top = false );

    /// <summary>
    /// Creates a new identifiable segment of code inside this code and returns it.
    /// This throws an <see cref="InvalidOperationException"/> if a part with the same key already exists.
    /// </summary>
    /// <param name="key">The <see cref="ITSKeyedCodePart.Key"/>.</param>
    /// <param name="closer"><see cref="Closer"/> of the subordinate part.</param>
    /// <param name="top">
    /// Optionally creates the new part at the start of the code instead of at the
    /// current writing position in the code.
    /// </param>
    /// <returns>The code part to use.</returns>
    ITSKeyedCodePart CreateKeyedPart( object key, string closer = "", bool top = false );

    /// <summary>
    /// Finds or creates an identifiable segment of code inside this code.
    /// </summary>
    /// <param name="key">The <see cref="ITSKeyedCodePart.Key"/>.</param>
    /// <param name="closer">
    /// <see cref="Closer"/> of the subordinate part.
    /// When not null, it must be the same as the Closer of the existing part if it has been already created.
    /// When let to null, it lets the existing Closer as-is if the part exists or defaults to the empty string if
    /// the part must be created.
    /// </param>
    /// <param name="top">
    /// Optionally creates the new part at the start of the code instead of at the
    /// current writing position in the code.
    /// </param>
    /// <returns>The code part with the key.</returns>
    ITSKeyedCodePart FindOrCreateKeyedPart( object key, string? closer = null, bool top = false );

    /// <summary>
    /// Finds an existing segment of code inside this code.
    /// </summary>
    /// <param name="key">The <see cref="ITSKeyedCodePart.Key"/>.</param>
    /// <returns>The keyed code part or null.</returns>
    ITSKeyedCodePart? FindKeyedPart( object key );

    /// <summary>
    /// Collects the whole code into a <see cref="StringBuilder"/>, optionally closing the
    /// scope with the <see cref="Closer"/> or leaving it opened.
    /// </summary>
    /// <param name="b">The string builder to write to.</param>
    /// <param name="closeScope">True to close the scope before returning the builder.</param>
    /// <returns>The string builder.</returns>
    StringBuilder Build( StringBuilder b, bool closeScope );

    /// <summary>
    /// Gets the current text.
    /// </summary>
    /// <returns>The current text (with the <see cref="Closer"/>). Can be empty.</returns>
    string ToString();

}
