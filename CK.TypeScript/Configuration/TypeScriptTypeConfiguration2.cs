using CK.Core;
using CK.TypeScript;
using System;

namespace CK.Setup;

/// <summary>
/// Unitary Type configuration. See <see cref="TypeScriptBinPathAspectConfiguration.Types"/>.
/// </summary>
/// <param name="Required">True for <see cref="RegistrationMode.Required"/>, false for <see cref="RegistrationMode.Regular"/>.</param>
/// <param name="Configuration">
/// The configuration to apply to the type. When specified, this overrides the <see cref="TypeScriptTypeAttribute2"/> that may
/// decorate the type.
/// </param>
public sealed record class TypeScriptTypeConfiguration2( bool Required, TypeScriptTypeAttribute2? Configuration = null );
