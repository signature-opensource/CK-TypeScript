using System;
using System.Collections.Generic;
using System.Text;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Extends <see cref="ITSCodeWriter"/> to support multiple parts, an optional <see cref="Closer"/> suffix and a <see cref="Memory"/>.
    /// </summary>
    public interface ITSCodePart : ITSCodeWriter
    {
        /// <summary>
        /// Gets the optional string that closes this scoped part.
        /// Defaults to the empty string.
        /// </summary>
        string Closer { get; }

        /// <summary>
        /// Gets an optional memory associated to this part.
        /// </summary>
        IDictionary<object, object?> Memory { get; }

        /// <summary>
        /// Creates a segment of code inside this code.
        /// </summary>
        /// <param name="closer">Optional <see cref="Closer"/> of the subordinate part.</param>
        /// <param name="top">
        /// Optionally creates the new part at the start of the code instead of at the
        /// current writing position in the code.
        /// </param>
        /// <returns>The code part to use.</returns>
        ITSCodePart CreatePart( string closer = "", bool top = false );

        /// <summary>
        /// Creates a named segment of code inside this code.
        /// This throws an <see cref="InvalidOperationException"/> if a part with the same name already exists.
        /// </summary>
        /// <param name="name">The <see cref="ITSNamedCodePart.Name"/>.</param>
        /// <param name="closer"><see cref="Closer"/> of the subordinate part.</param>
        /// <param name="top">
        /// Optionally creates the new part at the start of the code instead of at the
        /// current writing position in the code.
        /// </param>
        /// <returns>The code part to use.</returns>
        ITSNamedCodePart CreateNamedPart( string name, string closer = "", bool top = false );

        /// <summary>
        /// Finds or creates a named segment of code inside this code.
        /// </summary>
        /// <param name="name">The <see cref="ITSNamedCodePart.Name"/>.</param>
        /// <param name="closer">
        /// <see cref="Closer"/> of the subordinate part.
        /// When not null, it must be the same as the Closer of the existing part if it has been already crated.
        /// When let to null, it lets the exisiting Closer as-is if the part exists or defaults to the empty string if
        /// the part must be created.
        /// </param>
        /// <param name="top">
        /// Optionally creates the new part at the start of the code instead of at the
        /// current writing position in the code.
        /// </param>
        /// <returns>The code part to use.</returns>
        ITSNamedCodePart FindOrCreateNamedPart( string name, string? closer = null, bool top = false );

        /// <summary>
        /// Finds an existing named segment of code inside this code.
        /// </summary>
        /// <param name="name">The <see cref="ITSNamedCodePart.Name"/>.</param>
        /// <returns>The named code part or null.</returns>
        ITSNamedCodePart? FindNamedPart( string name );

        /// <summary>
        /// Collects the whole code into a string collector, optionnaly closing the
        /// scope with the <see cref="Closer"/> or leaving it opened.
        /// </summary>
        /// <param name="collector">The string collector to write to.</param>
        /// <param name="closeScope">True to close the scope.</param>
        void Build( Action<string> collector, bool closeScope );

        /// <summary>
        /// Collects the whole code into a <see cref="StringBuilder"/>, optionnaly closing the
        /// scope with the <see cref="Closer"/> or leaving it opened.
        /// </summary>
        /// <param name="b">The string builder to write to.</param>
        /// <param name="closeScope">True to close the scope before returning the builder.</param>
        /// <returns>The string builder.</returns>
        StringBuilder Build( StringBuilder b, bool closeScope );

    }

}
