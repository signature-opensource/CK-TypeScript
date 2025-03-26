using CK.Core;
using Microsoft.VisualBasic;
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
    /// Gets the culture of this set.
    /// </summary>
    ActiveCulture Culture { get; }

    /// <summary>
    /// Gets whether at least one of the <see cref="FinalTranslationValue"/> from these translations
    /// or from a child has a non null <see cref="FinalTranslationValue.Ambiguities"/>.
    /// </summary>
    bool IsAmbiguous { get; }

    /// <summary>
    /// Gets the parent translation set (less specific culture).
    /// </summary>
    IFinalTranslationSet? Parent { get; }

    /// <summary>
    /// Gets the children translations (more specific culture translations).
    /// </summary>
    IEnumerable<IFinalTranslationSet> Children { get; }

    /// <summary>
    /// Returns translations from these <see cref="Translations"/> and any other
    /// translations from <see cref="Parent"/> up to the <see cref="ActiveCultureSet.Root"/>.
    /// </summary>
    IEnumerable<KeyValuePair<string, FinalTranslationValue>> RootPropagatedTranslations { get; }
}
