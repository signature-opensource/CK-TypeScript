using CK.Core;
using System;
using System.Diagnostics.CodeAnalysis;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Default implementation of <see cref="ITSType"/>.
    /// This can be specialized.
    /// </summary>
    public class TSType : ITSType
    {
        /// <summary>
        /// Simple null wrapper. May be specialized if needed.
        /// </summary>
        protected class Null : ITSType
        {
            readonly ITSType _nonNullable;

            public Null( ITSType nonNullable )
            {
                _nonNullable = nonNullable;
                TypeName = nonNullable.TypeName + "|undefined";
            }

            public string TypeName { get; }

            public bool IsNullable => true;

            public bool HasDefaultValue => _nonNullable.HasDefaultValue;

            public string? DefaultValueSource => _nonNullable.DefaultValueSource;

            public Action<ITSFileImportSection>? RequiredImports => _nonNullable.RequiredImports;

            public ITSType Nullable => this;

            public ITSType NonNullable => _nonNullable;

            /// <summary>
            /// Overridden to return the <see cref="TypeName"/>.
            /// </summary>
            /// <returns>The <see cref="TypeName"/>.</returns>
            public override string ToString() => TypeName;

            public bool TryWriteValue( ITSCodeWriter writer, object value ) => _nonNullable.TryWriteValue( writer, value );
        }

        [AllowNull]
        readonly ITSType _null;

        /// <summary>
        /// Initializes a new <see cref="TSType"/>.
        /// </summary>
        /// <param name="typeName">The type name.</param>
        /// <param name="imports">The required imports.</param>
        /// <param name="defaultValue">The type default value if any.</param>
        public TSType( string typeName, Action<ITSFileImportSection>? imports, string? defaultValue )
            : this( imports, typeName, defaultValue )
        {
            _null = new Null( this );
        }

        /// <summary>
        /// Protected constructor.
        /// </summary>
        /// <param name="typeName">The type name.</param>
        /// <param name="imports">The required imports.</param>
        /// <param name="defaultValue">The type default value if any.</param>
        /// <param name="nullFactory">The <see cref="Nullable"/> factory.</param>
        protected TSType( string typeName, Action<ITSFileImportSection>? imports, string? defaultValue, Func<TSType, ITSType> nullFactory )
            : this( imports, typeName, defaultValue )
        {
            _null = nullFactory( this );
        }

        TSType( Action<ITSFileImportSection>? imports, string typeName, string? defaultValue )
        {
            Throw.CheckNotNullOrWhiteSpaceArgument( typeName );
            TypeName = typeName;
            RequiredImports = imports;
            DefaultValueSource = defaultValue;
        }

        /// <inheritdoc />
        public string TypeName { get; }

        /// <inheritdoc />
        public bool IsNullable => false;

        /// <inheritdoc />
        public bool HasDefaultValue => !string.IsNullOrEmpty( DefaultValueSource );

        /// <inheritdoc />
        public string? DefaultValueSource { get; }

        /// <inheritdoc />
        public Action<ITSFileImportSection>? RequiredImports { get; }

        /// <inheritdoc />
        public ITSType Nullable => _null;

        /// <inheritdoc />
        public ITSType NonNullable => this;

        /// <inheritdoc />
        public bool TryWriteValue( ITSCodeWriter writer, object value )
        {
            if( DoTryWriteValue( writer, value ) )
            {
                RequiredImports?.Invoke( writer.File.Imports );
                return true;
            }
            return false;
        }

        /// <summary>
        /// Implements the <see cref="TryWriteValue(ITSCodeWriter, object)"/>.
        /// </summary>
        /// <remarks>
        /// The default implementation here always returns false.
        /// </remarks>
        /// <param name="writer">The target writer.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>True if this type has been able to write the value, false otherwise.</returns>
        protected virtual bool DoTryWriteValue( ITSCodeWriter writer, object value ) => false;

        /// <summary>
        /// Overridden to return the <see cref="TypeName"/>.
        /// </summary>
        /// <returns>The <see cref="TypeName"/>.</returns>
        public override string ToString() => TypeName;
    }

}

