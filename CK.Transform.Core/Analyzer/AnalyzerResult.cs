using CK.Core;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Transform.Core;

/// <summary>
/// Extends <see cref="IAnalyzerResult{T}"/> and provides helpers.
/// </summary>
public static class AnalyzerResult
{
    /// <summary>
    /// Ensures that the <see cref="IAnalyzerResult{T}.Success"/> is true and handles any exception.
    /// </summary>
    /// <typeparam name="T">The analyzer's result type.</typeparam>
    /// <param name="analyzer">This analyzer.</param>
    /// <param name="monitor">Required monitor.</param>
    /// <param name="text">The text to analyze.</param>
    /// <returns>The result on success, null on any failure.</returns>
    public static IAnalyzerResult<T>? SafeParse<T>( this IAnalyzer<T> analyzer, IActivityMonitor monitor, ReadOnlyMemory<char> text ) where T : class
    {
        try
        {
            var r = analyzer.Parse( text );
            if( !r.Success )
            {
                var error = r.Error;
                if( error == null ) error = r.Tokens.OfType<TokenError>().First();
                monitor.Error( $"""
                        Parsing error {error.ErrorMessage} - @{error.SourcePosition.Line},{error.SourcePosition.Column} while parsing:
                        {text}
                        """ );
                return null;
            }
            return r;
        }
        catch( Exception e )
        {
            monitor.Error( $"""
                        Unexpected '{e.Message}' while parsing:
                        {text}
                        """ );
            return null;
        }
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <typeparam name="T">The analyzer's result type.</typeparam>
    /// <param name="tokens">The tokens. If <paramref name="error"/> is null, at least one <see cref="TokenError"/> must exist.</param>
    /// <param name="error">The error.</param>
    /// <returns>A failed result.</returns>
    public static IAnalyzerResult<T> CreateFailed<T>( ImmutableArray<Token> tokens, TokenError? error ) where T : class
    {
        return new FailedResult<T>() { Tokens = tokens, Error = error };
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <typeparam name="T">The analyzer's result type.</typeparam>
    /// <param name="tokens">The tokens. There should be no <see cref="TokenError"/>.</param>
    /// <param name="result">The associated result.</param>
    /// <returns>A successful analysis result.</returns>
    public static IAnalyzerResult<T> Create<T>( ImmutableArray<Token> tokens, T? result ) where T : class
    {
        return new SuccessResult<T>() { Tokens = tokens, Result = result };
    }

    sealed class FailedResult<T> : IAnalyzerResult<T> where T : class
    {
        public bool Success => false;

        public T? Result => null;

        public TokenError? Error { get; init; }

        public ImmutableArray<Token> Tokens { get; init; }

        object? IAnalyzerResult.Result => null;
    }

    sealed class SuccessResult<T> : IAnalyzerResult<T> where T : class
    {
        public bool Success => true;

        public T? Result { get; init; }

        public TokenError? Error => null;

        public ImmutableArray<Token> Tokens { get; init; }

        object? IAnalyzerResult.Result => Result;
    }

}
