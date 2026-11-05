using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.Setup;

/// <summary>
/// Contributes a feature to the generated "ck-gen/ts-locales/locales.ts" file. A package brings its feature (a
/// framework's locale data, a translation library, a component library's culture...) through a contributor registered
/// with <see cref="TypeScriptContext.AddLocalesContributor(ResPackage, TSLocalesContributor)"/>: the engine only writes
/// the generic part (<c>loadTranslations</c>, <c>LocaleInfo</c>, <c>CKLocales</c>, <c>locales</c> and
/// <c>DEFAULT_LOCALE_INFO</c>).
/// <para>
/// The file is written in this order, each part in the order of the contributors (the order of their packages):
/// the <see cref="AddImports"/> lines, the <see cref="WritePrelude"/> codes, the generic part with the
/// <see cref="LocaleInfoFields"/> (in the <c>LocaleInfo</c> type and in the value of each active culture),
/// then the <see cref="WriteEpilogue"/> codes.
/// </para>
/// </summary>
public abstract class TSLocalesContributor
{
    /// <summary>
    /// Adds the import lines of this contribution. Each line is written followed by a new line.
    /// Does nothing by default.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="activeCultures">The active cultures.</param>
    /// <param name="imports">The collector of import lines.</param>
    public virtual void AddImports( IActivityMonitor monitor, ActiveCultureSet activeCultures, List<string> imports )
    {
    }

    /// <summary>
    /// Writes code after all the imports, before the generic part (registering the cultures in a framework for
    /// instance). The code is written as is: it must end with its own new lines.
    /// Does nothing by default.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="activeCultures">The active cultures.</param>
    /// <param name="defaultCulture">The default culture.</param>
    /// <param name="code">The code to append to.</param>
    /// <returns>True on success, false on error (errors must be logged).</returns>
    public virtual bool WritePrelude( IActivityMonitor monitor,
                                      ActiveCultureSet activeCultures,
                                      NormalizedCultureInfo defaultCulture,
                                      StringBuilder code )
    {
        return true;
    }

    /// <summary>
    /// Gets the fields that this contribution adds to the <c>LocaleInfo</c> type. Empty by default.
    /// </summary>
    public virtual IReadOnlyList<TSLocaleInfoField> LocaleInfoFields => [];

    /// <summary>
    /// Writes code after <c>DEFAULT_LOCALE_INFO</c> (a service that applies the current locale for instance). The code
    /// is written as is: it must end with its own new lines.
    /// Does nothing by default.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="activeCultures">The active cultures.</param>
    /// <param name="defaultCulture">The default culture.</param>
    /// <param name="code">The code to append to.</param>
    /// <returns>True on success, false on error (errors must be logged).</returns>
    public virtual bool WriteEpilogue( IActivityMonitor monitor,
                                       ActiveCultureSet activeCultures,
                                       NormalizedCultureInfo defaultCulture,
                                       StringBuilder code )
    {
        return true;
    }
}

/// <summary>
/// A field of the generated <c>LocaleInfo</c> type (see <see cref="TSLocalesContributor.LocaleInfoFields"/>).
/// </summary>
public sealed class TSLocaleInfoField
{
    readonly Func<IActivityMonitor, NormalizedCultureInfo, string?> _getValue;

    /// <summary>
    /// Initializes a new field.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <param name="typeScriptType">The TypeScript type of the field.</param>
    /// <param name="getValue">
    /// Gets the TypeScript expression of the field's value for a culture (a string literal must be quoted).
    /// Null is an error that must be logged.
    /// </param>
    public TSLocaleInfoField( string name, string typeScriptType, Func<IActivityMonitor, NormalizedCultureInfo, string?> getValue )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( name );
        Throw.CheckNotNullOrWhiteSpaceArgument( typeScriptType );
        Throw.CheckNotNullArgument( getValue );
        Name = name;
        TypeScriptType = typeScriptType;
        _getValue = getValue;
    }

    /// <summary>
    /// Gets the field name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the TypeScript type of the field.
    /// </summary>
    public string TypeScriptType { get; }

    /// <summary>
    /// Gets the TypeScript expression of the field's value for a culture.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="culture">The culture.</param>
    /// <returns>The value expression or null on error (errors are logged).</returns>
    public string? GetValue( IActivityMonitor monitor, NormalizedCultureInfo culture ) => _getValue( monitor, culture );
}
