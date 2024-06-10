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
        public IEnumerable<ITSDeclaredFileType> AllTypes  => _declaredOnlyTypes != null ? _declaredOnlyTypes.Concat( LocalTypes ) : LocalTypes;

        /// <summary>
        /// Gets the all the TypeScript types that are defined in this <see cref="File"/> that have
        /// a <see cref="ITSFileType.TypePart"/>.
        /// </summary>
        public IEnumerable<ITSFileType> AllTypesWithPart => LocalTypes;

        /// <summary>
        /// Gets the TypeScript types bound to a C# type that are defined in this <see cref="File"/>.
        /// </summary>
        public IEnumerable<ITSFileCSharpType> CSharpTypes => LocalTypes.Where( t => t.Type != null );

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
            return new TSLocalType( this, typeName, additionalImports, null, defaultValueSource, closer );
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
        public ITSFileCSharpType CreateType( string typeName,
                                             Type type,
                                             Action<ITSFileImportSection>? additionalImports,
                                             string? defaultValueSource,
                                             string closer = "}\n" )
        {
            Throw.CheckNotNullOrWhiteSpaceArgument( typeName );
            Throw.CheckNotNullArgument( type );
            var t = new TSLocalType( this, typeName, additionalImports, type, defaultValueSource, closer );
            _typeManager.RegisterType( type, t );
            return t;
        }

        IEnumerable<TSLocalType> LocalTypes => _file.Body.Parts.OfType<ITSKeyedCodePart>()
                                                          .Select( p => p.Key as TSLocalType )
                                                          .Where( k => k != null )!;

        // Pure TS type declaration without a specific TypePart in the file.
        sealed class TSDeclaredType : TSBasicType, ITSDeclaredFileType
        {
            readonly TSManualFile _file;

            public TSDeclaredType( TSManualFile file, string typeName, Action<ITSFileImportSection>? additionalImports, string? defaultValueSource )
                : base( file._typeManager, typeName, additionalImports, defaultValueSource )
            {
                _file = file;
            }

            public override TypeScriptFile File => _file._file;
        }


        // This is a "potential" ITSFileCSharpType only if Type is not null.
        // Otherwise it is considered a ITSFileType: we always have the File and the TypePart
        // and we use the KeyedTypePart with this to handle these types registration.
        sealed class TSLocalType : TSBasicType, ITSFileCSharpType
        {
            readonly TSManualFile _file;
            public readonly ITSKeyedCodePart Part;
            public readonly Type? Type;

            public TSLocalType( TSManualFile file,
                                string typeName,
                                Action<ITSFileImportSection>? additionalImports,
                                Type? type,
                                string? defaultValueSource,
                                string closer )
                : base( file._typeManager, typeName, additionalImports, defaultValueSource )
            {
                _file = file;
                Part = file.File.Body.CreateKeyedPart( this, closer );
                Type = type;
            }

            Type ITSFileCSharpType.Type => Type!;

            public override TypeScriptFile File => _file._file;

            public ITSCodePart TypePart => Part;

            public override void EnsureRequiredImports( ITSFileImportSection section )
            {
                base.EnsureRequiredImports( section );
                section.EnsureImport( _file._file, TypeName );
            }
        }
    }
}

