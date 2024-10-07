using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Helper to build documentation. This object is always available on <see cref="TypeScriptRoot.DocBuilder"/>
/// even if <see cref="GenerateDocumentation"/> is false.
/// </summary>
public sealed class DocumentationBuilder
{
    readonly StringBuilder _b;
    readonly bool _withStars;
    readonly bool _generateDoc;
    string? _finalResult;
    int _removeGetOrSetPrefix;
    bool _lastLineIsEmpty;
    bool _waitingForNewline;

    /// <summary>
    /// Initializes a new documentation builder.
    /// </summary>
    /// <param name="withStars">False to let the text naked. By default, a star comment is generated.</param>
    /// <param name="generateDoc">False if no doc should be generated.</param>
    public DocumentationBuilder( bool withStars = true, bool generateDoc = true )
    {
        _withStars = withStars;
        _generateDoc = generateDoc;
        if( withStars )
        {
            _b = new StringBuilder( "/**" );
            _waitingForNewline = true;
        }
        else
        {
            _b = new StringBuilder();
        }
    }

    /// <summary>
    /// Resets this builder so it can be reused.
    /// </summary>
    /// <returns>This builder.</returns>
    public DocumentationBuilder Reset()
    {
        _lastLineIsEmpty = false;
        _finalResult = null;
        _b.Clear();
        if( _withStars )
        {
            _b.Append( "/**" );
            _waitingForNewline = true;
        }
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
    public bool IsEmpty => _b.Length == (_withStars ? 3 : 0);

    /// <summary>
    /// Temporarily removes any "Gets or sets " prefix of text fragments.
    /// This applies to the basic <see cref="Append(string?, bool, bool)"/> method that is
    /// called by all the other methods.
    /// </summary>
    public IDisposable RemoveGetOrSetPrefix()
    {
        ++_removeGetOrSetPrefix;
        return Util.CreateDisposableAction( () => --_removeGetOrSetPrefix );
    }

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
                if( _withStars ) _b.Append( Environment.NewLine ).Append( " **/" );
                _finalResult = _b.ToString();
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

        foreach( var x in DistinctByValue( xDoc.Elements().Where( e => e.Name.LocalName == "summary" || e.Name.LocalName == "value" ) ) )
        {
            AppendLinesFromXElement( source, x, true, true );
        }
        foreach( var rem in DistinctByValue( xDoc.Elements( "remarks" ) ) )
        {
            AppendEmptyLine();
            AppendLinesFromXElement( source, rem, true, true );
        }
        foreach( var typeParam in xDoc.Elements( "typeparam" ).GroupBy( eP => eP.Attribute( "name" )?.Value ) )
        {
            if( typeParam.Key == null ) continue;
            Append( $"@typeParam {typeParam.Key} ", true );
            bool isNext = false;
            foreach( var e in DistinctByValue( typeParam ) )
            {
                AppendLinesFromXElement( source, e, true, startNewLine: isNext );
                isNext = true;
            }
        }
        foreach( var param in xDoc.Elements( "param" ).GroupBy( eP => eP.Attribute( "name" )?.Value ) )
        {
            if( param.Key == null ) continue;
            Append( $"@param {param.Key} ", true );
            bool isNext = false;
            foreach( var e in DistinctByValue( param ) )
            {
                AppendLinesFromXElement( source, e, true, isNext );
                isNext = true;
            }
        }
        var ret = xDoc.Elements( "returns" );
        if( ret.Any() )
        {
            Append( "@returns ", true );
            bool isNext = false;
            foreach( var r in DistinctByValue( ret ) )
            {
                AppendLinesFromXElement( source, r, true, isNext );
                isNext = true;
            }
        }
        return this;
    }

    sealed class TrimAndIgnoreCase : EqualityComparer<ReadOnlyMemory<char>>
    {
        public override bool Equals( ReadOnlyMemory<char> x, ReadOnlyMemory<char> y )
        {
            return x.Span.Trim().Equals( y.Span.Trim(), StringComparison.OrdinalIgnoreCase );
        }

        public override int GetHashCode( [DisallowNull] ReadOnlyMemory<char> obj )
        {
            return string.GetHashCode( obj.Span, StringComparison.OrdinalIgnoreCase );
        }
    }

    static IEnumerable<XElement> DistinctByValue( IEnumerable<XElement> elements )
    {
        var already = new HashSet<ReadOnlyMemory<char>>( TrimAndIgnoreCase.Default );
        foreach( var e in elements )
        {
            if( already.Add( e.Value.AsMemory() ) )
            {
                yield return e;
            }
        }
    }

    /// <summary>
    /// Appends lines from an Xml element: &lt;c&gt;, &lt;code&gt; are transformed to ` and ``` markdown
    /// markers, &lt;para&gt; introduces a blank line, code references are processed by the <see cref="TypeScriptRoot.DocumentationCodeRefHandler"/>
    /// obtained from the <paramref name="source"/> and name attribute of &lt;paramref&gt;/&lt;typeparamref&gt; are extracted.
    /// Any other elements are handled as "transparent" elements.
    /// Consecutive empty lines are collapsed into a single empty line.
    /// </summary>
    /// <param name="source">Source code file that is used to locate and use the <see cref="IXmlDocumentationCodeRefHandler"/>.</param>
    /// <param name="e">The documentation element. Ignored when null.</param>
    /// <param name="trimFirstLine">False to keep all leading white spaces.</param>
    /// <param name="startNewLine">False to append the first line to the end of the current last line.</param>
    /// <returns>This builder to enable fluent syntax.</returns>
    public DocumentationBuilder AppendLinesFromXElement( TypeScriptFile source, XElement? e, bool trimFirstLine = true, bool startNewLine = true )
    {
        CheckBuilderClosed();
        if( e != null )
        {
            foreach( var n in e.Nodes() )
            {
                // Comments, document type or processing instructions are ignored.
                if( n is XElement c )
                {
                    switch( c.Name.LocalName )
                    {
                        case "code":
                            Append( "```", true );
                            AppendText( c.Value, false, true, true, true );
                            AppendLine( "```", true );
                            trimFirstLine = startNewLine = true;
                            break;
                        case "c":
                            Append( "`", false );
                            AppendText( c.Value, false, true, false, false );
                            Append( "`", false );
                            trimFirstLine = startNewLine = false;
                            break;
                        case "para":
                            AppendEmptyLine();
                            AppendLinesFromXElement( source, c, true, true );
                            trimFirstLine = startNewLine = true;
                            break;
                        case "seealso":
                        case "see":
                        {
                            var cref = (string)c.AttributeRequired( "cref" );
                            char kind = cref[0];
                            if( kind == '!' )
                            {
                                Append( $"~~{cref}~~" );
                            }
                            else
                            {
                                string tName;
                                string? mName;
                                if( kind == 'T' )
                                {
                                    tName = cref.Substring( 2 );
                                    mName = null;
                                }
                                else
                                {
                                    if( kind != 'P' && kind != 'F' && kind != 'M' && kind != 'E' )
                                    {
                                        throw new CKException( $"Unsupported cref '{cref}' in element '{e}'." );
                                    }
                                    int iPar = cref.IndexOf( '(' );
                                    if( iPar < 0 ) iPar = cref.Length;
                                    int idx = cref.LastIndexOf( '.', iPar - 1 );
                                    tName = cref.Substring( 2, idx - 2 );
                                    ++idx;
                                    mName = cref.Substring( idx, iPar - idx );
                                    if( mName == "#ctor" ) mName = "constructor";
                                }
                                var h = source.Folder.Root.DocumentationCodeRefHandler ?? DocumentationCodeRef.TextOnly;
                                Append( h.GetTSDocLink( source, kind, tName, mName, c.Value ) );
                            }
                            trimFirstLine = startNewLine = false;
                            break;
                        }
                        case "paramref":
                        case "typeparamref":
                            Append( (string)c.AttributeRequired( "name" ) );
                            trimFirstLine = startNewLine = false;
                            break;
                        default:
                            // Any other elements are "transparent": their lines are lifted .
                            AppendLinesFromXElement( source, c, true, true );
                            trimFirstLine = startNewLine = true;
                            break;
                    }
                }
                else if( n is XText t )
                {
                    AppendText( t.Value, trimFirstLine, true, startNewLine, false );
                }
            }
        }
        return this;
    }

    /// <summary>
    /// Appends multiple lines at once, removing their common white spaces prefix.
    /// See <see cref="AppendText(string, bool, bool, bool, bool)"/> to append a block of text.
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
        foreach( var l in lines )
        {
            Append( l, startNewLine, false );
            startNewLine = true;
        }
        _waitingForNewline |= endWithNewline;
        return this;
    }

    /// <summary>
    /// Appends a multi lines text, removing common white spaces prefix from each line.
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
        if( text != null )
        {
            var l = text.Split( new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries );
            if( l.Length > 0 )
            {
                if( startNewLine )
                {
                    int p = ComputeSpacePrefix( l, false );
                    AppendLines( l, false, p, trimFirstLine );
                }
                else
                {
                    // In "inline" mode (when startNewLine is false), we normalize
                    // whitespaces to be one.
                    // If trimFirstLine is specified, we remove all the white spaces. 
                    var firstLine = l[0];
                    int p = ComputeSpacePrefixLine( firstLine );
                    if( p >= 0 )
                    {
                        if( p > 0 && !trimFirstLine ) --p;
                        Append( firstLine.Substring( p ), false, true );
                    }
                    if( l.Length > 1 )
                    {
                        AppendLines( l, true, ComputeSpacePrefix( l, true ), false );
                    }
                }
                if( trimLastLines ) _lastLineIsEmpty = false;
                _waitingForNewline |= endWithNewline;
            }
        }
        return this;

        static int ComputeSpacePrefix( string[] lines, bool skipFirstLine )
        {
            Debug.Assert( !skipFirstLine || lines.Length > 1 );
            int spacePrefix = 0;
            for(; ; )
            {
                int hasContent = lines.Length;
                bool skip = skipFirstLine;
                if( skip ) --hasContent;
                foreach( var line in lines )
                {
                    if( skip )
                    {
                        skip = false;
                        continue;
                    }
                    if( spacePrefix >= line.Length ) --hasContent;
                    else if( !char.IsWhiteSpace( line, spacePrefix ) ) return spacePrefix;
                }
                if( hasContent == 0 ) return -1;
                ++spacePrefix;
            }
        }

        static int ComputeSpacePrefixLine( string line )
        {
            for( int i = 0; i < line.Length; ++i )
            {
                if( !Char.IsWhiteSpace( line, i ) ) return i;
            }
            return -1;
        }

        void AppendLines( string[] lines, bool skipFirstLine, int spacePrefix, bool trimFirstLine )
        {
            if( spacePrefix >= 0 )
            {
                foreach( var line in lines )
                {
                    if( skipFirstLine )
                    {
                        skipFirstLine = false;
                        continue;
                    }
                    if( spacePrefix < line.Length )
                    {
                        var l = line.Substring( spacePrefix );
                        if( trimFirstLine ) l = l.TrimStart();

                        Append( l, true, false );
                    }
                    else
                    {
                        AppendEmptyLine();
                    }
                    trimFirstLine = false;
                }
            }
        }
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
        if( String.IsNullOrEmpty( lineFragment ) )
        {
            // Adding an empty string is void except if
            // it implies newlines.
            if( startNewLine && !_lastLineIsEmpty )
            {
                _lastLineIsEmpty = true;
                _waitingForNewline = true;
            }
        }
        else
        {
            if( _waitingForNewline || startNewLine )
            {
                if( _lastLineIsEmpty )
                {
                    _b.Append( Environment.NewLine );
                    if( _withStars ) _b.Append( " * " );
                }
                _b.Append( Environment.NewLine );
                if( _withStars ) _b.Append( " * " );
                _waitingForNewline = false;
            }
            if( _removeGetOrSetPrefix > 0 )
            {
                lineFragment = RemoveGetsOrSetsPrefix( lineFragment );
            }
            _b.Append( lineFragment );
            _lastLineIsEmpty = false;
        }
        _waitingForNewline |= endWithNewline;
        return this;
    }

    void CheckBuilderClosed()
    {
        if( _finalResult != null ) throw new InvalidOperationException( nameof( GetFinalText ) );
    }

    static readonly Regex _rGetSet = new Regex( @"^\s*Gets?(\s+(or\s+)?sets?)?\s+", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase );

    static string RemoveGetsOrSetsPrefix( string text )
    {
        var m = _rGetSet.Match( text );
        if( m.Success )
        {
            text = text.Substring( m.Length );
            text = TypeScriptRoot.ToIdentifier( text, true );
        }
        return text;
    }

}
