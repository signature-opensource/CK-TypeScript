using CK.Core;
using System;
using System.Diagnostics.CodeAnalysis;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Default implementation of <see cref="ITSType"/>.
    /// <para>
    /// This concrete class can be be used for TypeScript types that don't have an associated <see cref="File"/>.
    /// It is not linked to a specific C# type (as opposed to a <see cref="ITSGeneratedType"/> that handles C# type
    /// in a <see cref="TypeScriptFile"/>).
    /// </para>
    /// <para>
    /// It can be instantiated directly thanks to the public <see cref="TSType(string,Action{ITSFileImportSection},string)"/> constructor
    /// or a <see cref="ITSTypeBuilder"/> can be used for more complex cases.
    /// </para>
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

            public bool HasDefaultValue => true;

            public string? DefaultValueSource => "undefined";

            public void EnsureRequiredImports( ITSFileImportSection section ) => _nonNullable.EnsureRequiredImports( section );

            public ITSType Nullable => this;

            public ITSType NonNullable => _nonNullable;

            public TypeScriptFile? File => _nonNullable.File;

            /// <summary>
            /// Overridden to return the <see cref="TypeName"/>.
            /// </summary>
            /// <returns>The <see cref="TypeName"/>.</returns>
            public override string ToString() => TypeName;

            public bool TryWriteValue( ITSCodeWriter writer, object? value )
            {
                if( value == null )
                {
                    writer.Append( "undefined" );
                    return true;
                }
                return _nonNullable.TryWriteValue( writer, value );
            }
        }

        [AllowNull]
        readonly ITSType _null;
        readonly string _typeName;
        readonly string? _defaultValueSource;
        readonly Action<ITSFileImportSection>? _requiredImports;

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
        /// <param name="imports">Optional required imports to use this type.</param>
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
            _typeName = typeName;
            _requiredImports = imports;
            _defaultValueSource = defaultValue;
        }

        /// <inheritdoc />
        public string TypeName => _typeName;

        /// <inheritdoc />
        public bool IsNullable => false;

        /// <inheritdoc />
        public bool HasDefaultValue => !string.IsNullOrEmpty( DefaultValueSource );

        /// <inheritdoc />
        public string? DefaultValueSource => _defaultValueSource;

        /// <summary>
        /// Gets a null file at this level.
        /// This is overridden by <see cref="ITSGeneratedType"/> implementations.
        /// </summary>
        public virtual TypeScriptFile? File => null;

        /// <inheritdoc />
        public ITSType Nullable => _null;

        /// <inheritdoc />
        public ITSType NonNullable => this;

        /// <inheritdoc />
        public virtual void EnsureRequiredImports( ITSFileImportSection section )
        {
            Throw.CheckNotNullArgument( section );
            _requiredImports?.Invoke( section );
        }

        /// <inheritdoc />
        public bool TryWriteValue( ITSCodeWriter writer, object? value )
        {
            if( value != null && DoTryWriteValue( writer, value ) )
            {
                _requiredImports?.Invoke( writer.File.Imports );
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
        public override string ToString() => _typeName;
    }
}

