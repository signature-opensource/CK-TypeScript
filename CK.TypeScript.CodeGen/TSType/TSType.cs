using CK.Core;

namespace CK.TypeScript.CodeGen;

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

        /// <summary>
        /// Initialize the null type of a non nullable type.
        /// </summary>
        /// <param name="nonNullable"></param>
        public Null( ITSType nonNullable )
        {
            Throw.CheckNotNullArgument( nonNullable );
            Throw.CheckArgument( !nonNullable.IsNullable );
            _nonNullable = nonNullable;
            _typeName = nonNullable.TypeName + "|undefined";
            _optionalTypeName = nonNullable.TypeName + "?";
        }

        /// <summary>
        /// Gets the <see cref="NonNullable"/> type name with "|undefined" suffix.
        /// </summary>
        public string TypeName => _typeName;

        /// <summary>
        /// Gets the <see cref="NonNullable"/> type name with "?" suffix.
        /// </summary>
        public string OptionalTypeName => _optionalTypeName;

        /// <summary>
        /// Always true.
        /// </summary>
        public bool IsNullable => true;

        /// <summary>
        /// Always "undefined".
        /// </summary>
        public string? DefaultValueSource => "undefined";

        /// <summary>
        /// Relays to <see cref="NonNullable"/>'s EnsureRequiredImports.
        /// </summary>
        /// <param name="section">The target import section.</param>
        public void EnsureRequiredImports( ITSFileImportSection section ) => _nonNullable.EnsureRequiredImports( section );

        /// <summary>
        /// Gets this instance.
        /// </summary>
        public ITSType Nullable => this;

        /// <summary>
        /// Gets the non nullable type.
        /// </summary>
        public ITSType NonNullable => _nonNullable;

        /// <summary>
        /// Gets the negative of the <see cref="NonNullable"/> index.
        /// </summary>
        public int Index => -_nonNullable.Index;

        /// <summary>
        /// Gets the <see cref="NonNullable"/>'s model.
        /// </summary>
        public ITSCodePart? TSTypeModel => NonNullable.TSTypeModel;

        /// <summary>
        /// Overridden to return the <see cref="TypeName"/>.
        /// </summary>
        /// <returns>The <see cref="TypeName"/>.</returns>
        public override string ToString() => TypeName;

        /// <summary>
        /// If <paramref name="value"/> is null, writes "undefined", otherwise relays to the <see cref="NonNullable"/>'s TryWriteValue.
        /// </summary>
        /// <param name="writer">The target writer.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>True if the value has been written, false otherwise.</returns>
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
    readonly ITSCodePart? _model;
    readonly int _index;

    /// <summary>
    /// Initializes a new <see cref="TSType"/>.
    /// The <paramref name="typeName"/> must not already exist in the <paramref name="typeManager"/>.
    /// </summary>
    /// <param name="typeManager">The type manager.</param>
    /// <param name="typeName">The type name.</param>
    public TSType( TSTypeManager typeManager, string typeName )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( typeName );
        _typeName = typeName;
        _null = new Null( this );
        _index = typeManager.Register( this, out _model );
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
    public virtual IMinimalTypeScriptFile? File => null;

    /// <inheritdoc />
    public int Index => _index;

    /// <inheritdoc />
    public ITSCodePart? TSTypeModel => _model;

    /// <inheritdoc />
    public ITSType Nullable => _null;

    /// <inheritdoc />
    public ITSType NonNullable => this;

    /// <inheritdoc />
    /// <remarks>
    /// At this level, this does nothing: this applies to any type that is a pure signature
    /// that doesn't require any imports (like a <c>Array&lt;number&gt;</c>).
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

