using CK.Core;
using System;
using System.Linq;

namespace CK.Transform.Core;

/// <summary>
/// Extends <see cref="IAnalyzer"/>.
/// </summary>
public static class AnalyzerExtensions
{
    /// <summary>
    /// Ensures that the <see cref="AnalyzerResult.Success"/> is true and handles any exception.
    /// </summary>
    /// <param name="analyzer">This analyzer.</param>
    /// <param name="monitor">Required monitor.</param>
    /// <param name="text">The text to analyze.</param>
    /// <returns>The result on success, null on any failure.</returns>
    public static AnalyzerResult? SafeParse( this IAnalyzer analyzer, IActivityMonitor monitor, ReadOnlyMemory<char> text )
    {
        try
        {
            var r = analyzer.Parse( text );
            if( !r.Success )
            {
                var error = r.Error ?? r.SourceCode.Tokens.OfType<TokenError>().First();
                monitor.Error( $"""
                        Parsing error {error.ErrorMessage} - @{error.SourcePosition.Line},{error.SourcePosition.Column} while parsing:
                        {text}
                        """ );
                return null;
            }
            return r;
        }
        catch( Exception ex )
        {
            monitor.Error( $"""
                        Unexpected error while parsing:
                        {text}
                        """, ex );
            return null;
        }
    }
}
