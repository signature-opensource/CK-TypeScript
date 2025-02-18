using System;

namespace CK.Core;

/// <summary>
/// A translation value is a <see cref="Text"/> that comes from an <see cref="Origin"/>.
/// <para>
/// Equality ignores the <see cref="Origin"/> and <see cref="Override"/>: two values are equal if their <see cref="Text"/> are equal.
/// </para>
/// </summary>
public readonly struct TranslationValue : IEquatable<TranslationValue>
{
    readonly string _text;
    readonly ResourceLocator _origin;
    readonly ResourceOverrideKind _override;

    /// <summary>
    /// Gets the translation text.
    /// </summary>
    public string Text => _text;

    /// <summary>
    /// Gets the origin of the translation.
    /// </summary>
    public ResourceLocator Origin => _origin;

    /// <summary>
    /// Gets whether this translation overrides an existing translation provided by another component.
    /// See <see cref="ResourceOverrideKind"/>.
    /// </summary>
    public ResourceOverrideKind Override => _override;

    /// <summary>
    /// Initializes a new translation value.
    /// </summary>
    /// <param name="text">The translation text.</param>
    /// <param name="origin">The origin.</param>
    /// <param name="overrideKind">Whether this translation overrides an existing translation provided by another component</param>
    public TranslationValue( string text, ResourceLocator origin, ResourceOverrideKind overrideKind )
    {
        _text = text;
        _origin = origin;
        _override = overrideKind;
    }

    /// <summary>
    /// Check whether <see cref="Text"/> are equal. <see cref="Origin"/> is ignored.
    /// </summary>
    /// <param name="other">The other value.</param>
    /// <returns>Whether this value has the same text as the onter one.</returns>
    public bool Equals( TranslationValue other ) => Text == other.Text;

    public override bool Equals( object? obj ) => obj is TranslationValue v && Equals( v );

    public override string ToString() => Text;

    public override int GetHashCode() => Text.GetHashCode();

    public static bool operator ==( TranslationValue x, TranslationValue y ) => x.Equals( y );

    public static bool operator !=( TranslationValue x, TranslationValue y ) => !x.Equals( y );

}
