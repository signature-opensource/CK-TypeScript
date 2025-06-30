using System;
using System.Diagnostics.CodeAnalysis;

namespace CK.Core;

public sealed partial class ResPackageDescriptor
{
    /// <summary>
    /// Union type of a <see cref="ResPackageDescriptor"/>, <see cref="string"/> or <see cref="Type"/>
    /// and optionality.
    /// <para>
    /// Implicit conversion operators are available.
    /// </para>
    /// </summary>
    public readonly struct Ref
    {
        internal readonly object? _ref;
        readonly bool _optional;

        /// <summary>
        /// Gets the invalid, required, reference. This is the <c>default</c>.
        /// </summary>
        public static Ref Invalid => default;

        /// <summary>
        /// Initializes a new <see cref="Ref"/>.
        /// </summary>
        /// <param name="fullName">The full name. Must not be empty or whitespace.</param>
        /// <param name="optional">True for an optional dependency.</param>
        public Ref( string fullName, bool optional = false )
        {
            Throw.CheckNotNullOrWhiteSpaceArgument( fullName );
            _ref = fullName;
            _optional = optional;
        }

        /// <summary>
        /// Initializes a new <see cref="Ref"/>.
        /// </summary>
        /// <param name="package">The existing package.</param>
        /// <param name="optional">True for an optional dependency.</param>
        public Ref( ResPackageDescriptor package, bool optional = false )
        {
            Throw.CheckNotNullArgument( package );
            _ref = package;
            _optional = optional;
        }

        /// <summary>
        /// Initializes a new <see cref="Ref"/>.
        /// </summary>
        /// <param name="type">The type that identifies the package.</param>
        /// <param name="optional">True for an optional dependency.</param>
        public Ref( Type type, bool optional = false )
        {
            Throw.CheckArgument( type?.FullName != null );
            _ref = type;
            _optional = optional;
        }

        /// <summary>
        /// Gets whether this reference is a valid one. 
        /// </summary>
        [MemberNotNullWhen( true, nameof( FullName ), nameof( _ref ) )]
        public bool IsValid => _ref != null;

        /// <summary>
        /// Gets whether this reference is optional.
        /// Defaults to false. Note that <see cref="IsValid"/> can be false and
        /// this can be true.
        /// </summary>
        public bool IsOptional => _optional;

        /// <summary>
        /// Gets whether this reference is an existing package descriptor.
        /// </summary>
        [MemberNotNullWhen( true, nameof( AsPackageDescriptor ) )]
        public bool IsPackageDescriptor => _ref is ResPackageDescriptor;

        /// <summary>
        /// Gets the referenced package if <see cref="IsPackageDescriptor"/> is true).
        /// </summary>
        public ResPackageDescriptor? AsPackageDescriptor => _ref as ResPackageDescriptor;

        /// <summary>
        /// Gets whether this reference is a string.
        /// </summary>
        public bool IsString => _ref is string;

        /// <summary>
        /// Gets whether this reference is a type.
        /// </summary>
        [MemberNotNullWhen( true, nameof( AsType ) )]
        public bool IsType => _ref is Type;

        /// <summary>
        /// Gets this reference as a type if <see cref="IsType"/> is true).
        /// </summary>
        public Type? AsType => _ref as Type;

        /// <summary>
        /// Gets the name of this reference.
        /// </summary>
        public string? FullName => _ref switch
        {
            string s => s,
            ResPackageDescriptor p => p.FullName,
            Type t => t.FullName,
            _ => null
        };

        public static implicit operator Ref( string fullName ) => new Ref( fullName );
        public static implicit operator Ref( ResPackageDescriptor package ) => new Ref( package );
        public static implicit operator Ref( Type type ) => new Ref( type );

        /// <summary>
        /// Overridden to return the <see cref="FullName"/> or the empty string if <see cref="IsValid"/> is false.
        /// </summary>
        /// <returns>The full name.</returns>
        public override string ToString() => FullName ?? "";
    }

}
