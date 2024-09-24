using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using static System.Net.WebRequestMethods;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// A TypeScript file resides in a definitive <see cref="TypeScriptFolder"/>
/// and exposes a <see cref="Imports"/> and a <see cref="Body"/> sections.
/// <para>
/// A TypeScriptFile can:
/// <list type="bullet">
///     <item>Contain any code in its <see cref="Body"/>.</item>
///     <item>Defines a TypeScript type name without code. See <see cref="ITSDeclaredFileType"/>.</item>
///     <item>Defines a TypeScript type and contains its code. See <see cref="ITSFileType"/>.</item>
///     <item>Defines the TypeScript type and contains the code of an existing C# type. See <see cref="ITSFileCSharpType"/>.</item>
/// </list>
/// </para>
/// </summary>
public sealed class TypeScriptFile : TextFileBase, IMinimalTypeScriptFile
{
    readonly ITSFileBodySection _body;
    internal readonly FileImportCodePart _imports;
    TypeDeclarationImpl _declared;

    internal TypeScriptFile( TypeScriptFolder folder, string name )
        : base( folder, name ) 
    {
        _imports = new FileImportCodePart( this );
        _body = new FileBodyCodePart( this );
    }

    internal TypeScriptFile( TypeScriptRoot r )
        : this( r.Root, _hiddenFileName ) 
    {
    }

    /// <summary>
    /// Gets the import section of this file.
    /// </summary>
    public ITSFileImportSection Imports => _imports;

    /// <summary>
    /// Gets the code section of this file.
    /// </summary>
    public ITSFileBodySection Body => _body;

    /// <summary>
    /// Creates a part that is bound to this file but whose content
    /// is not in this <see cref="Body"/>.
    /// </summary>
    /// <returns>A detached part.</returns>
    public ITSCodePart CreateDetachedPart() => new RawCodePart( this, String.Empty );

    /// <summary>
    /// Always null. To retrieve the <see cref="Imports"/> and <see cref="Body"/> use <see cref="GetCurrentText()"/>.
    /// </summary>
    /// <returns></returns>
    public override Stream? TryGetContentStream() => null;

    /// <summary>
    /// Overridden to compute and returns the concatenated <see cref="Imports"/> and <see cref="Body"/>.
    /// </summary>
    /// <returns>The full file content.</returns>
    public override string GetCurrentText()
    {
        var b = new StringBuilder();
        SmarterStringBuilder sB = new SmarterStringBuilder( b );
        _imports.Build( ref sB );
        if( b.Length > 0 ) b.Append( Environment.NewLine );
        // closeScope parameter is ignored here (a body has no closer).
        _body.Build( b, false );
        return b.ToString();
    }

    /// <summary>
    /// Gets the all the TypeScript types that are defined in this <see cref="File"/>.
    /// </summary>
    public IEnumerable<ITSDeclaredFileType> AllTypes => _declared.AllTypes.Concat( AllTypesWithPart );

    /// <summary>
    /// Gets the all the TypeScript types that have a <see cref="ITSFileType.TypePart"/> defined
    /// in this <see cref="File"/>.
    /// </summary>
    public IEnumerable<ITSFileType> AllTypesWithPart => _body.Parts.OfType<ITSKeyedCodePart>()
                                                                   .Select( p => p.Key as TSFileType )
                                                                   .Where( k => k != null )!;

    /// <summary>
    /// Gets the TypeScript types bound to a C# type that are defined in this <see cref="File"/>.
    /// </summary>
    public IEnumerable<ITSFileCSharpType> CSharpTypes => _body.Parts.OfType<ITSKeyedCodePart>()
                                                                    .Select( p => p.Key as TSCSharpType )
                                                                    .Where( k => k != null )!;

    /// <summary>
    /// Declares only a <see cref="ITSDeclaredFileType"/> in this file: the <paramref name="typeName"/> is implemented
    /// in this file but not in a specific <see cref="ITSCodePart"/>.
    /// <para>
    /// The <paramref name="typeName"/> must not already exist in the <see cref="TSTypeManager"/>.
    /// </para>
    /// </summary>
    /// <param name="typeName">The TypeScript type name.</param>
    /// <param name="additionalImports">The required imports. Null when using this type requires only this file.</param>
    /// <param name="defaultValueSource">The type default value if any.</param>
    /// <returns>A TS type in this file (but with no associated <see cref="ITSCodePart"/>).</returns>
    public ITSDeclaredFileType DeclareType( string typeName,
                                            Action<ITSFileImportSection>? additionalImports = null,
                                            string? defaultValueSource = null )
    {
        return _declared.DeclareType( this, typeName, additionalImports, defaultValueSource );
    }

    /// <summary>
    /// Creates a <see cref="ITSFileType"/> in this file. This TS type is not bound to a C# type.
    /// The <paramref name="typeName"/> must not already exist in the <see cref="TSTypeManager"/>.
    /// </summary>
    /// <param name="typeName">The type name.</param>
    /// <param name="additionalImports">The required imports. Null when using this type requires only this file.</param>
    /// <param name="defaultValueSource">The type default value if any.</param>
    /// <param name="closer">Closer of the part.</param>
    /// <returns>A TS type with its <see cref="ITSCodePart"/> in this file.</returns>
    public ITSFileType CreateType( string typeName,
                                   Action<ITSFileImportSection>? additionalImports,
                                   string? defaultValueSource,
                                   string closer = "}\n" )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( typeName );
        return new TSFileType( this, typeName, additionalImports, defaultValueSource, closer );
    }

    /// <summary>
    /// Creates a <see cref="ITSFileCSharpType"/> in this file.
    /// The <paramref name="typeName"/> and the <paramref name="type"/> must not already exist in the <see cref="TSTypeManager"/>.
    /// </summary>
    /// <param name="typeName">The type name.</param>
    /// <param name="type">The C# type.</param>
    /// <param name="additionalImports">The required imports. Null when using this type requires only this file.</param>
    /// <param name="defaultValueSource">The type default value if any.</param>
    /// <param name="closer">Closer of the part.</param>
    /// <returns>The type definition.</returns>
    public ITSFileCSharpType CreateCSharpType( string typeName,
                                               Type type,
                                               Action<ITSFileImportSection>? additionalImports,
                                               string? defaultValueSource,
                                               string closer = "}\n" )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( typeName );
        Throw.CheckNotNullArgument( type );
        var t = new TSCSharpType( this, typeName, additionalImports, type, defaultValueSource, closer );
        Root.TSTypes.RegisterType( type, t );
        return t;
    }

    // Extends TSDeclaredType: a TypePart exists.
    // We always have the File and the TypePart and we use the KeyedTypePart with this as a key
    // to handle these types registration.
    class TSFileType : TypeDeclarationImpl.TSDeclaredType, ITSFileType
    {
        public readonly ITSKeyedCodePart Part;

        public TSFileType( TypeScriptFile file,
                           string typeName,
                           Action<ITSFileImportSection>? additionalImports,
                           string? defaultValueSource,
                           string closer )
            : base( file, typeName, additionalImports, defaultValueSource )
        {
            Part = file.Body.CreateKeyedPart( this, closer );
        }

        public ITSCodePart TypePart => Part;

        TypeScriptFile ITSFileType.File => Unsafe.As<TypeScriptFile>( base.File );
    }

    // Extends a TSFileType to associate a C# type to this TS type.
    sealed class TSCSharpType : TSFileType, ITSFileCSharpType
    {
        public TSCSharpType( TypeScriptFile file,
                             string typeName,
                             Action<ITSFileImportSection>? additionalImports,
                             Type type,
                             string? defaultValueSource,
                             string closer )
            : base( file, typeName, additionalImports, defaultValueSource, closer )
        {
            Type = type;
        }

        public Type Type { get; }
    }
}
