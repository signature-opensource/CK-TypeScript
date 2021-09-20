using System;
using System.Collections.Generic;
using System.Text;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Default implementation of <see cref="IXmlDocumentationCodeRefHandler"/>.
    /// </summary>
    public class DocumentationCodeRef
    {
        /// <summary>
        /// Singleton instance to use for simple, text only reference.
        /// </summary>
        public static readonly IXmlDocumentationCodeRefHandler TextOnly = new TextOnlyHandler();

        /// <summary>
        /// Singleton instance to use for basic link (https://typedoc.org/guides/link-resolution/)
        /// where links are simple 'TYPE.MEMBER': type name (the leaf of the full type name) is expected to be available
        /// as-is in the TypeScript source context and member name follows the <see cref="TypeScriptRoot.PascalCase"/> option.
        /// </summary>
        public static readonly IXmlDocumentationCodeRefHandler BasicLink = new BasicLinkHandler();

        /// <summary>
        /// Helpers that extracts the leaf of the <paramref name="targetType"/> and append
        /// the <paramref name="targetMember"/>, following <see cref="TypeScriptRoot.PascalCase"/> convention.
        /// </summary>
        /// <param name="source">The source file.</param>
        /// <param name="targetType">The full target type name.</param>
        /// <param name="targetMember">The optional member name.</param>
        /// <returns>The formatted reference.</returns>
        public static string GetBasicLink( TypeScriptFile source, string targetType, string? targetMember )
        {
            return targetType.Substring( targetType.LastIndexOf( '.' ) + 1 )
                        + (!String.IsNullOrEmpty( targetMember )
                            ? "." + source.Folder.Root.ToIdentifier( targetMember )
                            : String.Empty);
        }

        class TextOnlyHandler : IXmlDocumentationCodeRefHandler
        {
            public string GetTSDocLink( TypeScriptFile source, char targetKind, string targetType, string? targetMember, string? elemenText )
            {
                return String.IsNullOrWhiteSpace( elemenText )
                        ? GetBasicLink( source, targetType, targetMember )
                        : elemenText;
            }
        }

        class BasicLinkHandler : IXmlDocumentationCodeRefHandler
        {
            public string GetTSDocLink( TypeScriptFile source, char targetKind, string targetType, string? targetMember, string? elemenText )
            {
                string link = GetBasicLink( source, targetType, targetMember );
                return String.IsNullOrWhiteSpace( elemenText )
                        ? $"{{@link {link}}}"
                        : $"{{@link {link} | {elemenText}}}";
            }
        }
    }
}
