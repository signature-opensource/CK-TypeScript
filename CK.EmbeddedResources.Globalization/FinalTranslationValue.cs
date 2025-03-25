using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.EmbeddedResources;

/// <summary>
/// A translation value is a <see cref="Text"/> that comes from an <see cref="Origin"/>
/// with potential <see cref="Ambiguities"/>.
/// <para>
/// Equality ignores the <see cref="Origin"/: two values are equal if their <see cref="Text"/> are equal.
/// </para>
/// </summary>
public readonly struct FinalTranslationValue : IEquatable<FinalTranslationValue>
{
    readonly string _text;
    readonly ResourceLocator _origin;
    readonly IEnumerable<FinalTranslationValue>? _ambiguities;

    /// <summary>
    /// Gets the translation text.
    /// This is empty even when <see cref="IsValid"/> is false.
    /// </summary>
    public string Text => _text ?? string.Empty;

    /// <summary>
    /// Gets the origin of the translation.
    /// </summary>
    public ResourceLocator Origin => _origin;

    /// <summary>
    /// Gets the translations that share the same key if any.
    /// </summary>
    public IEnumerable<FinalTranslationValue>? Ambiguities => _ambiguities;

    /// <summary>
    /// Gets whether this value is valid.
    /// Invalid value is the <c>default</c> (<see cref="Text"/> is the empty string).
    /// </summary>
    public bool IsValid => _text != null;

    /// <summary>
    /// Initializes a new translation.
    /// </summary>
    /// <param name="text">The translation text.</param>
    /// <param name="origin">The origin.</param>
    /// <param name="ambiguities">The <see cref="Ambiguities"/> is any.</param>
    public FinalTranslationValue( string text, ResourceLocator origin, IEnumerable<FinalTranslationValue>? ambiguities = null )
    {
        _text = text;
        _origin = origin;
        _ambiguities = ambiguities;
    }


    /// <summary>
    /// Returns a translation with a new <see cref="Ambiguities"/>.
    /// </summary>
    /// <param name="ambiguous">The translation that share the same key as this <see cref="Origin"/>.</param>
    /// <returns>A new resource asset or this one if <paramref name="locator"/> is already known.</returns>
    public FinalTranslationValue AddAmbiguity( FinalTranslationValue ambiguous )
    {
        if( ambiguous == this
            || (_ambiguities != null && _ambiguities.Contains( ambiguous )) )
        {
            return this;
        }
        return _ambiguities == null
            ? new FinalTranslationValue( _text, _origin, [ambiguous] )
            : new FinalTranslationValue( _text, _origin, _ambiguities.Append( ambiguous ) );
    }

    /// <summary>
    /// Adds multiple ambiguities at once.
    /// </summary>
    /// <param name="ambiguities">The ambiguities to add.</param>
    /// <returns>A new translation value or this one if all <paramref name="ambiguities"/> are already known.</returns>
    public FinalTranslationValue AddAmbiguities( IEnumerable<FinalTranslationValue>? ambiguities )
    {
        if( ambiguities == null ) return this;
        FinalTranslationValue f = this;
        foreach( var a in ambiguities )
        {
            f = f.AddAmbiguity( a );
        }
        return f;
    }

    /// <summary>
    /// Check whether <see cref="Text"/> are equal. <see cref="Origin"/> is ignored.
    /// </summary>
    /// <param name="other">The other value.</param>
    /// <returns>Whether this <see cref="Text"/> has the same text as the other one.</returns>
    public bool Equals( FinalTranslationValue other ) => _text == other._text;

    public override bool Equals( object? obj ) => obj is FinalTranslationValue v && Equals( v );

    public override string ToString() => Text;

    public override int GetHashCode() => Text.GetHashCode();

    public static bool operator ==( FinalTranslationValue x, FinalTranslationValue y ) => x.Equals( y );

    public static bool operator !=( FinalTranslationValue x, FinalTranslationValue y ) => !x.Equals( y );
}
