using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;

namespace CK.TypeScript.CodeGen
{
    class DocumentationBuilder
    {
        readonly StringBuilder _b;
        bool _lastLineIsEmpty;
        bool _waitingForNewline;
        string? _finalResult;

        public DocumentationBuilder()
        {
            _b = new StringBuilder( "/**" );
            _waitingForNewline = true;
        }

        /// <summary>
        /// Gets the final result and closes this builder.
        /// </summary>
        /// <returns>The final text.</returns>
        public string GetFinalText()
        {
            if( _finalResult == null )
            {
                if( _b.Length > 3 )
                {
                    _b.Append( Environment.NewLine ).Append( " **/" );
                    _finalResult = _b.ToString();
                }
                else
                {
                    _finalResult = String.Empty;
                }
            }
            return _finalResult;
        }

        /// <summary>
        /// Appends a mapping from C# Xml documentation
        /// (see https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments)
        /// into TS documentation (see https://typedoc.org/guides/doccomments).
        /// <para>
        /// Processed elements are &lt;summary&gt;, &lt;value&gt;, &lt;remarks&gt;, &lt;typeparam&gt;, &lt;param&gt; and &lt;returns&gt;.
        /// Parameters and returned value comments are emitted with @typeparam, @param and @returns.
        /// Each block is processed by <see cref="AppendLinesFromXElement"/>.
        /// </para>
        /// </summary>
        /// <param name="source">Source code writer.</param>
        /// <param name="xDoc">The Xml documentation element. Ignored when null.</param>
        /// <returns>This builder to enable fluent syntax.</returns>
        public DocumentationBuilder AppendDocumentation( ITSCodeWriter source, XElement? xDoc )
        {
            CheckBuilderClosed();
            if( xDoc == null ) return this;

            AppendLinesFromXElement( source, xDoc.Element( "summary" ), true, true );
            AppendLinesFromXElement( source, xDoc.Element( "value" ), true, true );
            foreach( var rem in xDoc.Elements( "remarks" ) )
            {
                AppendEmptyLine();
                AppendLinesFromXElement( source, rem, true, true );
            }
            foreach( var typeParam in xDoc.Elements( "typeparam" ) )
            {
                Append( $"@typeParam {typeParam.Attribute( "name" ).Value} ", true );
                AppendLinesFromXElement( source, typeParam, true, false );
            }
            foreach( var param in xDoc.Elements( "param" ) )
            {
                Append( $"@param {param.Attribute( "name" ).Value} ", true );
                AppendLinesFromXElement( source, param, true, false );
            }
            var ret = xDoc.Element( "returns" );
            if( ret != null )
            {
                Append( "@returns ", true );
                AppendLinesFromXElement( source, ret, true, false );
            }
            return this;
        }

        /// <summary>
        /// Appends lines from an Xml element: &lt;c&gt;, &lt;code&gt; are transformed to ` and ``` markdown
        /// markers, &lt;para&gt; introduces a blank line, code references are processed by the <see cref="TypeScriptRoot.DocumentationCodeRefHandler"/>
        /// obtained from the <paramref name="source"/> and name attribute of &lt;paramref&gt;/&lt;typeparamref&gt; are extracted.
        /// Any other elements are handled as "transparent" elements.
        /// Consecutive empty lines are collapsed into a single empty line.
        /// </summary>
        /// <param name="source">Source code writer.</param>
        /// <param name="e">The documentation element. Ignored when null.</param>
        /// <param name="trimFirstLine">True to remove all leading white spaces.</param>
        /// <param name="startNewLine">False to append the first line to the end of the current last line.</param>
        /// <returns>This builder to enable fluent syntax.</returns>
        public DocumentationBuilder AppendLinesFromXElement( ITSCodeWriter source, XElement? e, bool trimFirstLine, bool startNewLine )
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
                                AppendMultiLines( c.Value, false, true, true, true );
                                AppendLine( "```", true );
                                trimFirstLine = startNewLine = true;
                                break;
                            case "c":
                                Append( "`", false );
                                AppendMultiLines( c.Value, false, true, false, false );
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
                                    string tName;
                                    string? mName;
                                    if( kind == 'T' )
                                    {
                                        tName = cref.Substring( 2 );
                                        mName = null;
                                    }
                                    else
                                    {
                                        int iPar = cref.IndexOf( '(' );
                                        if( iPar < 0 ) iPar = cref.Length;
                                        int idx = cref.LastIndexOf( '.', iPar - 1 );
                                        tName = cref.Substring( 2, idx - 2 );
                                        ++idx;
                                        mName = cref.Substring( idx, iPar - idx );
                                        if( mName == "#ctor" ) mName = "constructor";
                                    }
                                    var h = source.File.Folder.Root.DocumentationCodeRefHandler ?? DocumentationCodeRef.TextOnly;
                                    Append( h.GetTSDocLink( source, kind, tName, mName, c.Value ) );
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
                        AppendMultiLines( t.Value, trimFirstLine, true, startNewLine, false );
                    }
                }
            }
            return this;
        }

        /// <summary>
        /// Appends multiple lines at once, removing their common white spaces prefix.
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
        public DocumentationBuilder AppendMultiLines( string text, bool trimFirstLine, bool trimLastLines, bool startNewLine, bool endWithNewline )
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
                        _b.Append( Environment.NewLine ).Append( " * " );
                    }
                    _b.Append( Environment.NewLine ).Append( " * " );
                    _waitingForNewline = false;
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

    }
}
