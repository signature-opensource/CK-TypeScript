using CK.Core;
using System;
using System.Runtime.CompilerServices;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Implements a locally implemented type (a <see cref="ITSGeneratedType"/>).
    /// This class is internal.
    /// </summary>
    sealed class TSGeneratedType : TSType, ITSGeneratedType
    {
        readonly Func<ITSCodeWriter, object, bool>? _tryWriteValue;
        internal readonly TSCodeGenerator? _codeGenerator;

        sealed class GeneratedNull : Null, ITSGeneratedType
        {
            public GeneratedNull( ITSType nonNullable )
                : base( nonNullable )
            {
            }

            new TSGeneratedType NonNullable => Unsafe.As<TSGeneratedType>( base.NonNullable );

            public TypeScriptFile File => NonNullable.File;

            ITSGeneratedType ITSGeneratedType.Nullable => this;

            ITSGeneratedType ITSGeneratedType.NonNullable => NonNullable;

            public Type Type => NonNullable.Type;

            public ITSKeyedCodePart? TypePart => NonNullable.TypePart;

            public bool HasCodeGenerator => NonNullable.HasCodeGenerator;

            public ITSKeyedCodePart EnsureTypePart( string closer = "}\n", bool top = false )
            {
                return NonNullable.EnsureTypePart( closer, top );
            }
        }

        public TSGeneratedType( Type t,
                                string typeName,
                                TypeScriptFile file,
                                string? defaultValue,
                                Func<ITSCodeWriter,object,bool>? tryWriteValue,
                                TSCodeGenerator? codeGenerator )
            : base( typeName, i => i.EnsureImport( file, typeName ), defaultValue, t => new GeneratedNull( t ) )
        {
            Throw.CheckNotNullArgument( t );
            Throw.CheckNotNullArgument( file );
            Type = t;
            File = file;
            _tryWriteValue = tryWriteValue;
            _codeGenerator = codeGenerator;
        }

        public Type Type { get; }

        public TypeScriptFile File { get; }

        public new ITSGeneratedType Nullable => (ITSGeneratedType)base.Nullable;

        public new ITSGeneratedType NonNullable => this;

        public bool HasCodeGenerator => _codeGenerator != null;

        protected override bool DoTryWriteValue( ITSCodeWriter writer, object value ) => _tryWriteValue?.Invoke( writer, value ) ?? false;

        public ITSKeyedCodePart? TypePart => File.Body.FindKeyedPart( Type );

        public ITSKeyedCodePart EnsureTypePart( string closer = "}\n", bool top = false ) => File.Body.FindOrCreateKeyedPart( Type, closer, top );

    }

}

