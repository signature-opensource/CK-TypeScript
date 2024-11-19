using CK.Core;
using System;

namespace CK.Core;

/// <summary>
/// A translation value is a <see cref="Text"/> that comes from an <see cref="Origin"/>.
/// <para>
/// Equality ignores the <see cref="Origin"/> and : two values are equal if their <see cref="Text"/> are equal.
/// </para>
/// </summary>
public readonly struct TranslationValue : IEquatable<TranslationValue>
{
    readonly string _text;
    readonly IResourceContainer _origin;
    readonly bool _isOverride;

    /// <summary>
    /// Gets the translation text.
    /// </summary>
    public string Text => _text;

    /// <summary>
    /// Gets the origin of the translation.
    /// </summary>
    public IResourceContainer Origin => _origin;

    /// <summary>
    /// Gets whether this translation overrides an existing translation provided by another component.
    /// </summary>
    public bool IsOverride => _isOverride;

    /// <summary>
    /// Initializes a new translation value.
    /// </summary>
    /// <param name="text">The translation text.</param>
    /// <param name="origin">The origin.</param>
    /// <param name="isOverride">Whether this translation overrides an existing translation provided by another component</param>
    public TranslationValue( string text, IResourceContainer origin, bool isOverride )
    {
        _text = text;
        _origin = origin;
        _isOverride = isOverride;
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
