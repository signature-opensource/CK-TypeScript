using CK.Core;
using System;

namespace CK.EmbeddedResources;

/// <summary>
/// A translation definition is a <see cref="Text"/> and a <see cref="Override"/>.
/// <para>
/// Equality ignores the <see cref="Override"/>: two definitions are equal if their <see cref="Text"/> are equal.
/// </para>
/// </summary>
public readonly struct TranslationDefinition : IEquatable<TranslationDefinition>
{
    readonly string _text;
    readonly ResourceOverrideKind _override;

    /// <summary>
    /// Gets the translation text.
    /// </summary>
    public string Text => _text;

    /// <summary>
    /// Gets whether this translation overrides an existing translation provided by another component.
    /// See <see cref="ResourceOverrideKind"/>.
    /// </summary>
    public ResourceOverrideKind Override => _override;

    /// <summary>
    /// Initializes a new translation value.
    /// </summary>
    /// <param name="text">The translation text.</param>
    /// <param name="overrideKind">Whether this translation overrides an existing translation provided by another component</param>
    public TranslationDefinition( string text, ResourceOverrideKind overrideKind )
    {
        _text = text;
        _override = overrideKind;
    }

    /// <summary>
    /// Check whether <see cref="Text"/> are equal. <see cref="Origin"/>  and <see cref="Override"/> are ignored.
    /// </summary>
    /// <param name="other">The other value.</param>
    /// <returns>Whether this <see cref="Text"/> has the same text as the other one.</returns>
    public bool Equals( TranslationDefinition other ) => Text == other.Text;

    public override bool Equals( object? obj ) => obj is TranslationDefinition v && Equals( v );

    public override string ToString() => Text;

    public override int GetHashCode() => Text.GetHashCode();

    public static bool operator ==( TranslationDefinition x, TranslationDefinition y ) => x.Equals( y );

    public static bool operator !=( TranslationDefinition x, TranslationDefinition y ) => !x.Equals( y );

}
