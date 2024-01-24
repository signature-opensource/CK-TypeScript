using CK.Core;
using System;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Base class for <see cref="ITSType"/> implementations.
    /// Handles nullable type (that may be specialized) and basic behavior.
    /// <para>
    /// Only the <see cref="DefaultValueSource"/> property must be implemented.
    /// </para>
    /// </summary>
    public abstract class TSType : ITSType
    {
        /// <summary>
        /// Simple null wrapper. May be specialized if needed.
        /// </summary>
        protected class Null : ITSType
        {
            readonly ITSType _nonNullable;
            readonly string _typeName;
            readonly string _optionalTypeName;

            public Null( ITSType nonNullable )
            {
                _nonNullable = nonNullable;
                _typeName = nonNullable.TypeName + "|undefined";
                _optionalTypeName = nonNullable.TypeName + "?";
            }

            public string TypeName => _typeName;

            public string OptionalTypeName => _optionalTypeName;

            public bool IsNullable => true;

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

        readonly ITSType _null;
        readonly string _typeName;

        /// <summary>
        /// Initializes a new <see cref="TSBasicType"/>.
        /// </summary>
        /// <param name="typeName">The type name.</param>
        public TSType( string typeName )
        {
            Throw.CheckNotNullOrWhiteSpaceArgument( typeName );
            _typeName = typeName;
            _null = new Null( this );
        }

        /// <summary>
        /// Initializes a new <see cref="TSBasicType"/> with a specialized typed
        /// associated null instance.
        /// </summary>
        /// <param name="typeName">The type name.</param>
        /// <param name="nullFactory">The null instance factory.</param>
        protected TSType( string typeName, Func<TSType, ITSType> nullFactory )
        {
            Throw.CheckNotNullOrWhiteSpaceArgument( typeName );
            _typeName = typeName;
            _null = nullFactory( this );
        }

        /// <inheritdoc />
        public string TypeName => _typeName;

        /// <inheritdoc />
        public string OptionalTypeName => _typeName;

        /// <inheritdoc />
        public bool IsNullable => false;

        /// <inheritdoc />
        public abstract string? DefaultValueSource { get; }

        /// <summary>
        /// Gets a null file at this level.
        /// </summary>
        public virtual TypeScriptFile? File => null;

        /// <inheritdoc />
        public ITSType Nullable => _null;

        /// <inheritdoc />
        public ITSType NonNullable => this;

        /// <inheritdoc />
        /// <remarks>
        /// At this level, this does nothing: this applies to any type that is a pure signature
        /// that doesn't require any imports (like a <c>number[]</c>).
        /// </remarks>
        public virtual void EnsureRequiredImports( ITSFileImportSection section )
        {
        }

        /// <inheritdoc />
        public bool TryWriteValue( ITSCodeWriter writer, object? value )
        {
            if( value != null && DoTryWriteValue( writer, value ) )
            {
                EnsureRequiredImports( writer.File.Imports );
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

