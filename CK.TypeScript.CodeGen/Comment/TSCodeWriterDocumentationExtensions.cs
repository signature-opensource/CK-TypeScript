using CK.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace CK.TypeScript.CodeGen
{
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
            return @this.NewLine().Append( new DocumentationBuilder().Append( lines, false, false ).GetFinalText() ).NewLine();
        }

        /// <summary>
        /// Appends a /** ... */ block of text with multiple lines automatically left aligned.
        /// See <see cref="DocumentationBuilder.AppendMultiLines(string, bool, bool, bool)"/>
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
            return @this.NewLine().Append( new DocumentationBuilder().AppendMultiLines( text, trimFirstLine, trimLastLines, false, false ).GetFinalText() ).NewLine();
        }

        /// <summary>
        /// See <see cref="DocumentationBuilder.AppendDocumentation(ITSCodeWriter, XElement?)"/>
        /// </summary>
        /// <typeparam name="T">Actual type of the code writer.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <param name="xDoc">The Xml documentation element. Ignored when null.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        public static T AppendDocumentation<T>( this T @this, XElement? xDoc ) where T : ITSCodeWriter
        {
            if( xDoc == null ) return @this;
            return @this.NewLine().Append( new DocumentationBuilder().AppendDocumentation( @this, xDoc ).GetFinalText() ).NewLine();
        }

        /// <summary>
        /// Appends the documentation of a type, method, property, event or constructor if <see cref="TypeScriptRoot.GenerateDocumentation"/> is true.
        /// See <see cref="DocumentationBuilder.AppendDocumentation(ITSCodeWriter, XElement?)"/>.
        /// </summary>
        /// <typeparam name="T">Actual type of the code writer.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="member">The member for which the documentation must be emitted.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        public static T AppendDocumentation<T>( this T @this, IActivityMonitor monitor, MemberInfo member ) where T : ITSCodeWriter
        {
            var xDoc = @this.File.Folder.Root.GenerateDocumentation
                            ? XmlDocumentationReader.GetXmlDocumentation( monitor, member.Module.Assembly, @this.File.Folder.Root.Memory )
                            : null;
            if( xDoc == null ) return @this;
            return AppendDocumentation( @this, xDoc, member );
        }

        /// <summary>
        /// Appends the documentation of a type, method, property, event or constructor.
        /// See <see cref="DocumentationBuilder.AppendDocumentation(ITSCodeWriter, XElement?)"/>.
        /// </summary>
        /// <typeparam name="T">Actual type of the code writer.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <param name="xmlDoc">The assembly Xml documentation file.</param>
        /// <param name="member">The member for which the documentation must be emitted.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        public static T AppendDocumentation<T>( this T @this, XDocument xmlDoc, MemberInfo member ) where T : ITSCodeWriter
        {
            if( xmlDoc == null ) throw new ArgumentNullException( nameof( xmlDoc ) );
            var xDoc = XmlDocumentationReader.GetDocumentationElement( xmlDoc, XmlDocumentationReader.GetNameAttributeValueFor( member ) );
            return xDoc != null ? AppendDocumentation<T>( @this, xDoc ) : @this;
        }

    }
}
