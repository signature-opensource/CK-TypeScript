using System;

namespace CK.Less.Transform;

/// <summary>
/// Defines the possible <c>@import (keyword) 'file';</c>.
/// </summary>
[Flags]
public enum ImportKeyword
{
    /// <summary>
    /// No import keyword.
    /// </summary>
    None = 0,

    /// <summary>
    /// Use a Less file but do not output it.
    /// </summary>
    Reference  = 1,

    /// <summary>
    /// Include the source file in the output but do not process it.
    /// </summary>
    Inline = 1 << 1,

    /// <summary>
    /// Treat the file as a Less file, no matter what the file extension.
    /// This excludes <see cref="Css"/>.
    /// </summary>
    Less = 1 << 2,

    /// <summary>
    /// Treat the file as a CSS file, no matter what the file extension.
    /// This excludes <see cref="Less"/>.
    /// </summary>
    Css = 1 << 3,

    /// <summary>
    /// Only include the file once (this is default behavior).
    /// This excludes <see cref="Multiple"/>.
    /// </summary>
    Once = 1 << 4,

    /// <summary>
    /// Include the file multiple times.
    /// This excludes <see cref="Once"/>.
    /// </summary>
    Multiple = 1 << 5,

    /// <summary>
    /// Continue compiling when file is not found.
    /// </summary>
    Optional = 1 << 6
}

public static class ImportKeywordExtensions
{
    public static ImportKeyword Normalize( this ImportKeyword k )
    {
        // Multiple has the priority over the 'once' default.
        if( (k & ImportKeyword.Multiple) != 0 ) k &= ~ImportKeyword.Once;
        if( (k & ImportKeyword.Once) != 0 ) k &= ~ImportKeyword.Multiple;

        // Less has the priority over 'css'.
        if( (k & ImportKeyword.Less) != 0 ) k &= ~ImportKeyword.Css;
        if( (k & ImportKeyword.Css) != 0 ) k &= ~ImportKeyword.Less;

        return k;
    }

    public static ImportKeyword Apply( this ImportKeyword k, ImportKeyword include, ImportKeyword exclude )
    {
        // Accounting the priorities.
        if( (include & ImportKeyword.Css) != 0 ) exclude |= ImportKeyword.Less;
        if( (include & ImportKeyword.Once) != 0 ) exclude |= ImportKeyword.Multiple;
        var newOne = (k & ~exclude) | include;
        return Normalize( newOne );
    }

}
