using CK.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Provides extension methods on <see cref="ITSCodeWriter"/> that helps
/// managing TypeScript comments generation.
/// </summary>
public static class TSCodeWriterDocumentationExtensions
{
    /// <summary>
    /// Appends a /** ... */ block of commented lines.
    /// </summary>
    /// <typeparam name="T">Actual type of the code writer.</typeparam>
    /// <param name="this">This code writer.</param>
    /// <param name="lines">Documentation lines to add.</param>
    /// <returns>This code writer to enable fluent syntax.</returns>
    public static T AppendDocumentation<T>( this T @this, IEnumerable<string> lines ) where T : ITSCodeWriter
    {
        return @this.Append( @this.File.Root.DocBuilder.Reset().Append( lines, false, false ).GetFinalText() ).NewLine();
    }

    /// <summary>
    /// Appends a /** ... */ block of text with multiple lines automatically left aligned.
    /// See <see cref="DocumentationBuilder.AppendText(string, bool, bool, bool, bool)"/>
    /// </summary>
    /// <typeparam name="T">Actual type of the code writer.</typeparam>
    /// <param name="this">This code writer.</param>
    /// <param name="text">Documentation text to add.</param>
    /// <param name="trimFirstLine">False to keep leading white spaces if any.</param>
    /// <param name="trimLastLines">False to keep empty last lines (as a single empty line since consecutive empty lines are collapsed).</param>
    /// <returns>This code writer to enable fluent syntax.</returns>
    public static T AppendDocumentation<T>( this T @this, string? text, bool trimFirstLine = true, bool trimLastLines = true ) where T : ITSCodeWriter
    {
        if( string.IsNullOrWhiteSpace( text ) ) return @this;
        return @this.Append( @this.File.Root.DocBuilder.Reset().AppendText( text, trimFirstLine, trimLastLines, false, false ).GetFinalText() );
    }

    /// <summary>
    /// See <see cref="DocumentationBuilder.AppendDocumentation(TypeScriptFile, IEnumerable{XElement})"/>
    /// </summary>
    /// <typeparam name="T">Actual type of the code writer.</typeparam>
    /// <param name="this">This code writer.</param>
    /// <param name="xDoc">The Xml documentation element. Ignored when null.</param>
    /// <param name="extension">Optional extension that can append documentation.</param>
    /// <returns>This code writer to enable fluent syntax.</returns>
    public static T AppendDocumentation<T>( this T @this, XElement? xDoc, Action<DocumentationBuilder>? extension = null ) where T : ITSCodeWriter
    {
        if( xDoc == null )
        {
            extension?.Invoke( @this.File.Root.DocBuilder.Reset() );
            return @this;
        }
        return AppendDocumentation( @this, new[] { xDoc }, extension );
    }

    /// <summary>
    /// See <see cref="DocumentationBuilder.AppendDocumentation(TypeScriptFile, IEnumerable{XElement})"/>
    /// </summary>
    /// <typeparam name="T">Actual type of the code writer.</typeparam>
    /// <param name="this">This code writer.</param>
    /// <param name="xDoc">The Xml documentation element. Ignored when null.</param>
    /// <param name="extension">Optional extension that can append documentation.</param>
    /// <returns>This code writer to enable fluent syntax.</returns>
    public static T AppendDocumentation<T>( this T @this, IEnumerable<XElement> xDoc, Action<DocumentationBuilder>? extension = null ) where T : ITSCodeWriter
    {
        var b = @this.File.Root.DocBuilder.Reset().AppendDocumentation( @this.File, xDoc );
        extension?.Invoke( b );
        var text = b.GetFinalText();
        if( text.Length == 0 ) return @this;
        return @this.Append( text ).NewLine();
    }

    /// <summary>
    /// Appends the documentation of a type, method, property, event or constructor.
    /// See <see cref="DocumentationBuilder.AppendDocumentation(TypeScriptFile, IEnumerable{XElement})"/>.
    /// </summary>
    /// <typeparam name="T">Actual type of the code writer.</typeparam>
    /// <param name="this">This code writer.</param>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="member">The member for which the documentation must be emitted.</param>
    /// <param name="extension">Optional extension that can append documentation (called whether member has documentation or not).</param>
    /// <returns>This code writer to enable fluent syntax.</returns>
    public static T AppendDocumentation<T>( this T @this, IActivityMonitor monitor, MemberInfo? member, Action<DocumentationBuilder>? extension = null ) where T : ITSCodeWriter
    {
        var b = @this.File.Root.DocBuilder;
        var xDoc = b.GenerateDocumentation && member != null
                        ? XmlDocumentationReader.GetXmlDocumentation( monitor, member.Module.Assembly, @this.File.Folder.Root.Memory )
                        : null;
        var xE = xDoc != null
                    ? XmlDocumentationReader.GetDocumentationElement( xDoc, XmlDocumentationReader.GetNameAttributeValueFor( member! ) )
                    : null;
        return AppendDocumentation( @this, xE, extension );
    }

    /// <summary>
    /// Appends the documentation of a type, method, property, event or constructor.
    /// See <see cref="DocumentationBuilder.AppendDocumentation(TypeScriptFile, IEnumerable{XElement})"/>.
    /// </summary>
    /// <typeparam name="T">Actual type of the code writer.</typeparam>
    /// <param name="this">This code writer.</param>
    /// <param name="xmlDoc">The assembly Xml documentation file that should contain the member's documentation.</param>
    /// <param name="member">The member for which the documentation must be emitted.</param>
    /// <param name="extension">Optional extension that can append documentation (called whether member has documentation or not).</param>
    /// <returns>This code writer to enable fluent syntax.</returns>
    public static T AppendDocumentation<T>( this T @this, XDocument xmlDoc, MemberInfo? member, Action<DocumentationBuilder>? extension = null ) where T : ITSCodeWriter
    {
        Throw.CheckNotNullArgument( xmlDoc );
        var xE = member != null
                    ? XmlDocumentationReader.GetDocumentationElement( xmlDoc, XmlDocumentationReader.GetNameAttributeValueFor( member ) )
                    : null;
        return AppendDocumentation( @this, xE, extension );
    }

    /// <summary>
    /// Appends the documentation of a type, method, property, event or constructor from multiples
    /// documentation elements that will be merged.
    /// See <see cref="DocumentationBuilder.AppendDocumentation(TypeScriptFile, IEnumerable{XElement})"/>.
    /// </summary>
    /// <typeparam name="T">Actual type of the code writer.</typeparam>
    /// <param name="this">This code writer.</param>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="members">The members for which the documentation must be emitted.</param>
    /// <param name="extension">Optional extension that can append documentation (called whether there are members or not).</param>
    /// <returns>This code writer to enable fluent syntax.</returns>
    public static T AppendDocumentation<T>( this T @this, IActivityMonitor monitor, IEnumerable<MemberInfo> members, Action<DocumentationBuilder>? extension = null ) where T : ITSCodeWriter
    {
        var elements = XmlDocumentationReader.GetDocumentationFor( monitor, members, @this.File.Folder.Root.Memory );
        return AppendDocumentation( @this, elements, extension );
    }

}
