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
    public static AnalyzerResult? TryParse( this IAnalyzer analyzer, IActivityMonitor monitor, ReadOnlyMemory<char> text )
    {
        try
        {
            var r = analyzer.Parse( text );
            if( !r.Success )
            {
                var error = r.Error ?? r.SourceCode.Tokens.OfType<TokenError>().First();
                monitor.Error( $"""
                        Parsing error: {error.ErrorMessage} - @{error.SourcePosition.Line + 1},{error.SourcePosition.Column + 1} while parsing:
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

    /// <summary>
    /// Throws an <see cref="System.IO.InvalidDataException"/> if the <see cref="AnalyzerResult.Success"/> is false.
    /// </summary>
    /// <param name="analyzer">This analyzer.</param>
    /// <param name="text">The text to analyze.</param>
    /// <returns>The parsed source code.</returns>
    public static SourceCode ParseOrThrow( this IAnalyzer analyzer, ReadOnlyMemory<char> text )
    {
        var r = analyzer.Parse( text );
        if( !r.Success )
        {
            var error = r.Error ?? r.SourceCode.Tokens.OfType<TokenError>().First();
            Throw.InvalidDataException( $"""
                    Parsing error {error.ErrorMessage} - @{error.SourcePosition.Line},{error.SourcePosition.Column} while parsing:
                    {text}
                    """ );
        }
        return r.SourceCode;
    }
}
