using CK.Core;
using CK.TypeScript;
using System;

namespace CK.Setup;

/// <summary>
/// Unitary Type configuration.
/// </summary>
/// <param name="Type">The Type to register.</param>
/// <param name="Explicit">True for <see cref="RegistrationMode.Explicit"/>, false for <see cref="RegistrationMode.Regular"/>.</param>
/// <param name="Configuration">
/// The configuration to apply to the type. When specified, this overrides the <see cref="TypeScriptTypeAttribute2"/> that may
/// decorate the type.
/// </param>
public sealed record class TypeScriptTypeConfiguration2( Type Type, bool Explicit, TypeScriptTypeAttribute2? Configuration = null );
