using CK.Core;
using System.Collections.Generic;

namespace CK.EmbeddedResources;


/// <summary>
/// Captures <see cref="Translations"/> for a <see cref="Culture"/> .
/// </summary>
public interface IFinalTranslationSet
{
    /// <summary>
    /// Gets the translations.
    /// </summary>
    IReadOnlyDictionary<string, FinalTranslationValue> Translations { get; }

    /// <summary>
    /// Gets whether at least one of the <see cref="FinalTranslationValue"/> from these translations
    /// or from a child has a non null <see cref="FinalTranslationValue.Ambiguities"/>.
    /// </summary>
    bool IsAmbiguous { get; }

    /// <summary>
    /// Gets the culture of this set.
    /// </summary>
    ActiveCulture Culture { get; }

    /// <summary>
    /// Gets the children translations (more specific culture translations).
    /// </summary>
    IEnumerable<IFinalTranslationSet> Children { get; }
}
