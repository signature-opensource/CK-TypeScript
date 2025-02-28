using CK.Core;
using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CK.Transform.Core;

/// <summary>
/// Extends <see cref="IAnalyzer"/>.
/// </summary>
public static class AnalyzerExtensions
{
    /// <summary>
    /// Tries to parse multiple <see cref="TopLevelSourceSpan"/>.
    /// Returns null on error.
    /// </summary>
    /// <param name="analyzer">This top-level analyzer.</param>
    /// <param name="monitor">Required monitor.</param>
    /// <param name="text">the text to parse.</param>
    /// <returns>The top-level spans or null on error.</returns>
    public static List<T>? TryParseMultiple<T>( this ITopLevelAnalyzer<T> analyzer,
                                                IActivityMonitor monitor,
                                                ReadOnlyMemory<char> text )
        where T : TopLevelSourceSpan
    {
        var result = new List<T>();
        for(; ; )
        {
            var r = analyzer.TryParse( monitor, text );
            if( r == null ) return null;
            // Parse success doesn't mean that a top-level construct has been parsed.
            var f = r.SourceCode.Spans.FirstOrDefault();
            Throw.DebugAssert( f == null || f is T );
            if( f == null || f is not T topLevel )
            {
                // No Transform function: if the EndOfInput has been reached, we are good (text is whitespace or comments).
                if( r.EndOfInput ) break;
                // But if the EndOfInput has not been reached, it means that there are tokens but they don't start with a 'create'.
                return null;
            }
            result.Add( topLevel );
            text = r.RemainingText;
        }
        return result;
    }

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
                var error = r.HardError ?? r.SourceCode.Tokens.OfType<TokenError>().First();
                monitor.Error( $"""
                        {error.ErrorMessage} @{error.SourcePosition.Line},{error.SourcePosition.Column} while parsing:
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
            var error = r.HardError ?? r.SourceCode.Tokens.OfType<TokenError>().First();
            Throw.InvalidDataException( $"""
                    Parsing error {error.ErrorMessage} - @{error.SourcePosition.Line},{error.SourcePosition.Column} while parsing:
                    {text}
                    """ );
        }
        return r.SourceCode;
    }

    /// <inheritdoc cref="IAnalyzer.Parse(ReadOnlyMemory{char})" />
    public static AnalyzerResult Parse( this IAnalyzer analyzer, string text ) => analyzer.Parse( text.AsMemory() );

    /// <inheritdoc cref="TryParse(IAnalyzer, IActivityMonitor, ReadOnlyMemory{char})" />
    public static AnalyzerResult? TryParse( this IAnalyzer analyzer, IActivityMonitor monitor, string text ) => TryParse( analyzer, monitor, text.AsMemory() );

    /// <inheritdoc cref="ParseOrThrow(IAnalyzer, ReadOnlyMemory{char})" />
    public static SourceCode ParseOrThrow( this IAnalyzer analyzer, string text ) => ParseOrThrow( analyzer, text.AsMemory() );
}
