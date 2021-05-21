using System;
using System.Collections.Generic;
using System.Text;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Enables documentation generation based on Xml documentation file
    /// to handle code references.
    /// <para>
    /// This is exposed by the <see cref="TypeScriptRoot.DocumentationCodeRefHandler"/> mutable property
    /// </para>
    /// </summary>
    /// <remarks>
    /// See https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments.
    /// </remarks>
    public interface IXmlDocumentationCodeRefHandler
    {
        /// <summary>
        /// Must compute the TS documentation text for a code reference (typically &lt;see cref="..." /&gt;).
        /// </summary>
        /// <remarks>
        /// Even if this origin source is not really used by the <see cref="DocumentationCodeRef"/> default
        /// implementations, this is useful if, with specialized implementations, a cross reference to the
        /// target must be emitted.
        /// </remarks>
        /// <param name="source">The documentation location.</param>
        /// <param name="targetKind">The target can be 'T', 'M', 'P', 'F' or 'E'.</param>
        /// <param name="targetType">The documentation target type in dotted notation (nested types also use a dot).</param>
        /// <param name="targetMember">For 'M', 'P', 'F' or 'E', this is the member name. Null for 'T'.</param>
        /// <param name="elemenText">Optional text defined in the reference.</param>
        /// <returns>The TypeScript fragment of comment to insert.</returns>
        string GetTSDocLink( TypeScriptFile source, char targetKind, string targetType, string? targetMember, string? elemenText );
    }
}
