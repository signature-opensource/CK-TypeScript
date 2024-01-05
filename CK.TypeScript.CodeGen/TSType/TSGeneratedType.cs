using CK.Core;
using System;
using System.Data;
using System.Runtime.CompilerServices;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// This class is internal: only the ITSGeneratedType is exposed.
    /// </summary>
    sealed class TSGeneratedType : TSType, ITSGeneratedType
    {
        readonly TSValueWriter? _tryWriteValue;
        internal readonly TSCodeGenerator? _codeGenerator;
        readonly Type _type;
        readonly TypeScriptFile _file;
        readonly bool _hasError;

        sealed class GeneratedNull : Null, ITSGeneratedType
        {
            public GeneratedNull( ITSType nonNullable )
                : base( nonNullable )
            {
            }

            new TSGeneratedType NonNullable => Unsafe.As<TSGeneratedType>( base.NonNullable );

            public new TypeScriptFile File => NonNullable.File;

            ITSGeneratedType ITSGeneratedType.Nullable => this;

            ITSGeneratedType ITSGeneratedType.NonNullable => NonNullable;

            public Type Type => NonNullable.Type;

            public ITSKeyedCodePart? TypePart => NonNullable.TypePart;

            public bool HasError => NonNullable.HasError;

            public ITSKeyedCodePart EnsureTypePart( string closer = "}\n", bool top = false )
            {
                return NonNullable.EnsureTypePart( closer, top );
            }
        }

        internal TSGeneratedType( Type t,
                                   string typeName,
                                   TypeScriptFile file,
                                   string? defaultValue,
                                   TSValueWriter? tryWriteValue,
                                   TSCodeGenerator? codeGenerator,
                                   bool hasError )
            : base( typeName, null, defaultValue, t => new GeneratedNull( t ) )
        {
            Throw.DebugAssert( t != null );
            Throw.DebugAssert( file != null );
            _type = t;
            _file = file;
            _tryWriteValue = tryWriteValue;
            _codeGenerator = codeGenerator;
            _hasError = hasError;
        }

        public Type Type => _type;

        public override TypeScriptFile File => _file;

        public bool HasError => _hasError;

        public new ITSGeneratedType Nullable => (ITSGeneratedType)base.Nullable;

        public new ITSGeneratedType NonNullable => this;

        public override void EnsureRequiredImports( ITSFileImportSection section )
        {
            section.EnsureImport( _file, TypeName );
        }

        protected override bool DoTryWriteValue( ITSCodeWriter writer, object value ) => _tryWriteValue?.Invoke( writer, this, value ) ?? false;

        public ITSKeyedCodePart? TypePart => File.Body.FindKeyedPart( Type );

        public ITSKeyedCodePart EnsureTypePart( string closer = "}\n", bool top = false ) => File.Body.FindOrCreateKeyedPart( Type, closer, top );

    }

}

