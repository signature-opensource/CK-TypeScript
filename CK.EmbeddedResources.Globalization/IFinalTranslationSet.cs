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
    /// Gets the culture of this set.
    /// </summary>
    ActiveCulture Culture { get; }

    /// <summary>
    /// Gets whether at least one of the <see cref="FinalTranslationValue"/> from these translations
    /// has a non null <see cref="FinalTranslationValue.Ambiguities"/>.
    /// <para>
    /// For the root <see cref="FinalTranslationSet"/>, this is also true when any of
    /// the <see cref="FinalTranslationSet.AllTranslationSets"/> is ambiguous (avan if the root Translations
    /// have no ambiguity).
    /// </para>
    /// </summary>
    bool IsAmbiguous { get; }

    /// <summary>
    /// Gets the parent translation set (less specific culture).
    /// </summary>
    IFinalTranslationSet? Parent { get; }

    /// <summary>
    /// Gets the children translations (more specific culture translations) that have at least one
    /// translations (translation sets without translations are not instantiated).
    /// </summary>
    IEnumerable<IFinalTranslationSet> Children { get; }

    /// <summary>
    /// Returns translations from these <see cref="Translations"/> and any other
    /// translations from <see cref="Parent"/> up to the <see cref="ActiveCultureSet.Root"/>.
    /// </summary>
    IEnumerable<KeyValuePair<string, FinalTranslationValue>> RootPropagatedTranslations { get; }
}
