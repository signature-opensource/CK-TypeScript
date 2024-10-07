using CK.Core;
using System;

namespace CK.TypeScript.CodeGen;


/// <summary>
/// Mutable method parameter, property or variable: a name bound to a type that
/// may be optional and may have a default value.
/// </summary>
public sealed class TypeScriptVarType
{
    string _name;
    ITSType _tSType;
    string? _defaultValue;

    /// <summary>
    /// Initializes a new TypeScript parameter, property or variable with an initial name and type.
    /// </summary>
    /// <param name="name">The initial variable name.</param>
    /// <param name="type">The initial variable's type.</param>
    public TypeScriptVarType( string name, ITSType type )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( name );
        Throw.CheckNotNullArgument( type );
        _name = name;
        _tSType = type;
    }

    /// <summary>
    /// Gets or sets the associated comment.
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Gets or sets the parameter, property or variable name.
    /// </summary>
    public string Name
    {
        get => _name;
        set
        {
            Throw.CheckNotNullOrWhiteSpaceArgument( value );
            _name = value;
        }
    }

    /// <summary>
    /// Gets or sets the parameter, property or variable type.
    /// </summary>
    public ITSType TSType
    {
        get => _tSType;
        set
        {
            Throw.CheckNotNullArgument( value );
            _tSType = value;
        }
    }

    /// <summary>
    /// Gets or sets an optional default value.
    /// Overrides and defaults to type's <see cref="ITSType.DefaultValueSource"/>
    /// </summary>
    public string? DefaultValueSource
    {
        get => _defaultValue ?? TSType.DefaultValueSource;
        set
        {
            if( string.IsNullOrWhiteSpace( value ) ) value = null;
            _defaultValue = value;
        }
    }

    /// <summary>
    /// Gets whether this <see cref="DefaultValueSource"/> is not null or empty.
    /// </summary>
    public bool HasDefaultValue => !String.IsNullOrEmpty( DefaultValueSource );

    /// <summary>
    /// Returns this declaration.
    /// </summary>
    /// <returns>A readable string.</returns>
    public override string ToString() => $"{Name}{(TSType.IsNullable || HasDefaultValue ? "?: " : ": ")}{TSType.NonNullable.TypeName}{(HasDefaultValue ? " = " + DefaultValueSource : string.Empty)}";

}

