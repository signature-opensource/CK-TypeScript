using CK.CodeGen;
using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Helper to build documentation. This object is always available on <see cref="TypeScriptRoot.DocBuilder"/>
/// even if <see cref="GenerateDocumentation"/> is false.
/// <para>
/// The Xml documentation to Markdown conversion is the one of <see cref="DocumentationTextBuilder"/> (CK.CodeGen):
/// this builder adds the TypeScript comment ("/**", " * " prefixed lines, " **/"), the @typeParam, @param and @returns
/// tags, and the code references of the <see cref="TypeScriptRoot.DocumentationCodeRefHandler"/>.
/// </para>
/// </summary>
public sealed class DocumentationBuilder
{
    readonly DocumentationTextBuilder _b;
    readonly bool _withStars;
    readonly bool _generateDoc;
    string? _finalResult;

    /// <summary>
    /// Initializes a new documentation builder.
    /// </summary>
    /// <param name="withStars">False to let the text naked. By default, a star comment is generated.</param>
    /// <param name="generateDoc">False if no doc should be generated.</param>
    public DocumentationBuilder( bool withStars = true, bool generateDoc = true )
    {
        _withStars = withStars;
        _generateDoc = generateDoc;
        _b = withStars ? new DocumentationTextBuilder( "/**", " * " ) : new DocumentationTextBuilder();
        _b.ThrowOnUnsupportedCodeRef = true;
    }

    /// <summary>
    /// Resets this builder so it can be reused.
    /// </summary>
    /// <returns>This builder.</returns>
    public DocumentationBuilder Reset()
    {
        _finalResult = null;
        _b.Reset();
        return this;
    }

    /// <summary>
    /// Gets whether documentation should be generated: this comes from the configuration
    /// and may be ignored.
    /// </summary>
    public bool GenerateDocumentation => _generateDoc;

    /// <summary>
    /// Gets whether at least one character has been added.
    /// </summary>
    public bool IsEmpty => _b.IsEmpty;

    /// <summary>
    /// Temporarily removes any "Gets or sets " prefix of text fragments.
    /// See <see cref="DocumentationTextBuilder.RemoveGetOrSetPrefix"/>.
    /// </summary>
    public IDisposable RemoveGetOrSetPrefix() => _b.RemoveGetOrSetPrefix();

    /// <summary>
    /// Gets the final result and closes this builder.
    /// </summary>
    /// <returns>The final text that may be the empty string if no documentation has been added.</returns>
    public string GetFinalText()
    {
        if( _finalResult == null )
        {
            if( IsEmpty )
            {
                _finalResult = String.Empty;
            }
            else
            {
                _finalResult = _withStars ? _b.ToString() + Environment.NewLine + " **/" : _b.ToString();
            }
        }
        return _finalResult;
    }

    /// <summary>
    /// Appends a mapping of any number of C# Xml documentation by merging their different parts.
    /// This adapts C# (see https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments)
    /// into TS documentation (see https://typedoc.org/guides/doccomments).
    /// <para>
    /// Processed elements are &lt;summary&gt;, &lt;value&gt;, &lt;remarks&gt;, &lt;typeparam&gt;, &lt;param&gt; and &lt;returns&gt;.
    /// Parameters and returned value comments are emitted with @typeparam, @param and @returns.
    /// Each block is processed by <see cref="AppendLinesFromXElement"/>.
    /// </para>
    /// </summary>
    /// <param name="source">Source code file that is used to locate and use the <see cref="IXmlDocumentationCodeRefHandler"/>.</param>
    /// <param name="xDoc">The Xml documentation element. Ignored when null.</param>
    /// <returns>This builder to enable fluent syntax.</returns>
    public DocumentationBuilder AppendDocumentation( TypeScriptFile source, IEnumerable<XElement> xDoc )
    {
        CheckBuilderClosed();
        var codeRef = GetCodeRefHandler( source );
        _b.AppendSummaryAndRemarks( xDoc, codeRef );
        foreach( var typeParam in xDoc.Elements( "typeparam" ).GroupBy( eP => eP.Attribute( "name" )?.Value ) )
        {
            if( typeParam.Key == null ) continue;
            _b.Append( $"@typeParam {typeParam.Key} ", true );
            bool isNext = false;
            foreach( var e in DocumentationTextBuilder.DistinctByValue( typeParam ) )
            {
                _b.AppendLinesFromXElement( e, codeRef, true, startNewLine: isNext );
                isNext = true;
            }
        }
        foreach( var param in xDoc.Elements( "param" ).GroupBy( eP => eP.Attribute( "name" )?.Value ) )
        {
            if( param.Key == null ) continue;
            _b.Append( $"@param {param.Key} ", true );
            bool isNext = false;
            foreach( var e in DocumentationTextBuilder.DistinctByValue( param ) )
            {
                _b.AppendLinesFromXElement( e, codeRef, true, isNext );
                isNext = true;
            }
        }
        var ret = xDoc.Elements( "returns" );
        if( ret.Any() )
        {
            _b.Append( "@returns ", true );
            bool isNext = false;
            foreach( var r in DocumentationTextBuilder.DistinctByValue( ret ) )
            {
                _b.AppendLinesFromXElement( r, codeRef, true, isNext );
                isNext = true;
            }
        }
        return this;
    }

    // The code references are handled by the TypeScriptRoot.DocumentationCodeRefHandler (a constructor is "constructor").
    static DocumentationTextBuilder.CodeRefHandler GetCodeRefHandler( TypeScriptFile source )
    {
        var h = source.Folder.Root.DocumentationCodeRefHandler ?? DocumentationCodeRef.TextOnly;
        return ( kind, type, member, text ) => h.GetTSDocLink( source, kind, type, member == "#ctor" ? "constructor" : member, text );
    }

    /// <summary>
    /// Appends lines from an Xml element (see <see cref="DocumentationTextBuilder.AppendLinesFromXElement"/>): the code
    /// references are processed by the <see cref="TypeScriptRoot.DocumentationCodeRefHandler"/> obtained from the
    /// <paramref name="source"/>.
    /// </summary>
    /// <param name="source">Source code file that is used to locate and use the <see cref="IXmlDocumentationCodeRefHandler"/>.</param>
    /// <param name="e">The documentation element. Ignored when null.</param>
    /// <param name="trimFirstLine">False to keep all leading white spaces.</param>
    /// <param name="startNewLine">False to append the first line to the end of the current last line.</param>
    /// <returns>This builder to enable fluent syntax.</returns>
    public DocumentationBuilder AppendLinesFromXElement( TypeScriptFile source, XElement? e, bool trimFirstLine = true, bool startNewLine = true )
    {
        CheckBuilderClosed();
        _b.AppendLinesFromXElement( e, GetCodeRefHandler( source ), trimFirstLine, startNewLine );
        return this;
    }

    /// <summary>
    /// Appends multiple lines at once, removing their common white spaces prefix.
    /// See <see cref="DocumentationTextBuilder.Append(IEnumerable{string}, bool, bool)"/>.
    /// </summary>
    /// <param name="lines">Documentation lines to add.</param>
    /// <param name="startNewLine">
    /// True to start a new line.
    /// False to append the first line to the end of the current last line.
    /// </param>
    /// <param name="endWithNewline">True to end the current line after the fragment.</param>
    /// <returns>This builder to enable fluent syntax.</returns>
    public DocumentationBuilder Append( IEnumerable<string> lines, bool startNewLine, bool endWithNewline )
    {
        CheckBuilderClosed();
        _b.Append( lines, startNewLine, endWithNewline );
        return this;
    }

    /// <summary>
    /// Appends a multi lines text, removing common white spaces prefix from each line.
    /// See <see cref="DocumentationTextBuilder.AppendText(string, bool, bool, bool, bool)"/>.
    /// </summary>
    /// <param name="text">Text to add.</param>
    /// <param name="trimFirstLine">True to remove all leading white spaces.</param>
    /// <param name="trimLastLines">True to ignore any empty last lines.</param>
    /// <param name="startNewLine">
    /// True to start a new line.
    /// False to append the first line of the text to the end of the current last line.
    /// </param>
    /// <param name="endWithNewline">True to end the current line after the fragment.</param>
    /// <returns>This builder to enable fluent syntax.</returns>
    public DocumentationBuilder AppendText( string text, bool trimFirstLine, bool trimLastLines, bool startNewLine, bool endWithNewline )
    {
        CheckBuilderClosed();
        _b.AppendText( text, trimFirstLine, trimLastLines, startNewLine, endWithNewline );
        return this;
    }

    /// <summary>
    /// Appends an empty line.
    /// Multiple empty lines are collapsed.
    /// </summary>
    /// <returns>This builder to enable fluent syntax.</returns>
    public DocumentationBuilder AppendEmptyLine() => Append( String.Empty, true, true );

    /// <summary>
    /// Appends a text fragment that must not contain newlines and ends it with a new line.
    /// </summary>
    /// <param name="lineFragment">A line or some text without newlines inside.</param>
    /// <param name="startNewLine">
    /// True to start a new line.
    /// False to append the fragment to the end of the current last line if any.
    /// </param>
    /// <returns>This builder to enable fluent syntax.</returns>
    public DocumentationBuilder AppendLine( string? lineFragment, bool startNewLine ) => Append( lineFragment, startNewLine, true );

    /// <summary>
    /// Appends a text fragment that must not contain newlines.
    /// </summary>
    /// <param name="lineFragment">A line or some text without newlines inside.</param>
    /// <param name="startNewLine">
    /// True to start a new line.
    /// False to append the fragment to the end of the current last line if any.
    /// </param>
    /// <param name="endWithNewline">True to end the current line after the fragment.</param>
    /// <returns>This builder to enable fluent syntax.</returns>
    public DocumentationBuilder Append( string? lineFragment, bool startNewLine = false, bool endWithNewline = false )
    {
        CheckBuilderClosed();
        _b.Append( lineFragment, startNewLine, endWithNewline );
        return this;
    }

    void CheckBuilderClosed()
    {
        if( _finalResult != null ) throw new InvalidOperationException( nameof( GetFinalText ) );
    }
}
