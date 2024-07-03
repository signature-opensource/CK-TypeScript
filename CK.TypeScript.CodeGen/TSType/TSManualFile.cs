using CK.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Simple file with manually defined types.
    /// Created by <see cref="TypeScriptFolder.FindOrCreateManualFile(NormalizedPath)"/>.
    /// <para>
    /// A manual file can contain purely exported <see cref="ITSDeclaredFileType"/>, types
    /// with a dedicated part (the <see cref="ITSFileType"/>) and types with a dedicated part
    /// and also bound to a C# type (the <see cref="ITSFileCSharpType"/>).
    /// <para>
    /// There is currently no <c>ITSCSharpTypeWithoutPart</c> that declares a TS type bound to a C# type
    /// without a corresponding part in a file because we don't need it. TS types bound to C# are built
    /// by resolving them with the help of the <see cref="RequireTSFromObjectEventArgs"/> or <see cref="RequireTSFromTypeEventArgs"/>.
    /// </para>
    /// </para>
    /// </summary>
    public sealed class TSManualFile 
    {
        readonly TSTypeManager _typeManager;
        readonly TypeScriptFile _file;
        List<ITSDeclaredFileType>? _declaredOnlyTypes;

        internal TSManualFile( TSTypeManager typeManager, TypeScriptFile file )
        {
            _typeManager = typeManager;
            _file = file;
        }

        /// <summary>
        /// Gets the file.
        /// </summary>
        public TypeScriptFile File => _file;

        /// <summary>
        /// Gets the all the TypeScript types that are defined in this <see cref="File"/>.
        /// </summary>
        public IEnumerable<ITSDeclaredFileType> AllTypes  => _declaredOnlyTypes != null ? _declaredOnlyTypes.Concat( AllTypesWithPart ) : AllTypesWithPart;

        /// <summary>
        /// Gets the all the TypeScript types that have a <see cref="ITSFileType.TypePart"/> defined
        /// in this <see cref="File"/>.
        /// </summary>
        public IEnumerable<ITSFileType> AllTypesWithPart => _file.Body.Parts.OfType<ITSKeyedCodePart>()
                                                                      .Select( p => p.Key as TSFileType )
                                                                      .Where( k => k != null )!;

        /// <summary>
        /// Gets the TypeScript types bound to a C# type that are defined in this <see cref="File"/>.
        /// </summary>
        public IEnumerable<ITSFileCSharpType> CSharpTypes => _file.Body.Parts.OfType<ITSKeyedCodePart>()
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
            Throw.CheckNotNullOrWhiteSpaceArgument( typeName );
            _declaredOnlyTypes ??= new List<ITSDeclaredFileType>();
            var t = new TSDeclaredType( this, typeName, additionalImports, defaultValueSource );
            _declaredOnlyTypes.Add( t );
            return t;
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
        /// <returns></returns>
        public ITSFileCSharpType CreateCSharpType( string typeName,
                                                   Type type,
                                                   Action<ITSFileImportSection>? additionalImports,
                                                   string? defaultValueSource,
                                                   string closer = "}\n" )
        {
            Throw.CheckNotNullOrWhiteSpaceArgument( typeName );
            Throw.CheckNotNullArgument( type );
            var t = new TSCSharpType( this, typeName, additionalImports, type, defaultValueSource, closer );
            _typeManager.RegisterType( type, t );
            return t;
        }

        // Pure TS type declaration without a specific TypePart in the file.
        class TSDeclaredType : TSBasicType, ITSDeclaredFileType
        {
            readonly TSManualFile _file;

            public TSDeclaredType( TSManualFile file, string typeName, Action<ITSFileImportSection>? additionalImports, string? defaultValueSource )
                : base( file._typeManager, typeName, additionalImports, defaultValueSource )
            {
                _file = file;
            }

            public override TypeScriptFile File => _file._file;

            public override void EnsureRequiredImports( ITSFileImportSection section )
            {
                base.EnsureRequiredImports( section );
                section.EnsureImport( _file._file, TypeName );
            }

        }


        // Extends TSDeclaredType: a TypePart exists.
        // We always have the File and the TypePart and we use the KeyedTypePart with this as a key
        // to handle these types registration.
        class TSFileType : TSDeclaredType, ITSFileType
        {
            public readonly ITSKeyedCodePart Part;

            public TSFileType( TSManualFile file,
                               string typeName,
                               Action<ITSFileImportSection>? additionalImports,
                               string? defaultValueSource,
                               string closer )
                : base( file, typeName, additionalImports, defaultValueSource )
            {
                Part = file.File.Body.CreateKeyedPart( this, closer );
            }

            public ITSCodePart TypePart => Part;
        }

        // Extends a TSFileType to associate a C# type to this TS type.
        sealed class TSCSharpType : TSFileType, ITSFileCSharpType
        {
            public TSCSharpType( TSManualFile file,
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
}

