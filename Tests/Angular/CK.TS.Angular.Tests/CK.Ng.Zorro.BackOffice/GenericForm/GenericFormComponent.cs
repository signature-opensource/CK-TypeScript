using CK.Core;
using CK.TS.Angular;
using CK.TypeScript;

namespace CK.Ng.Zorro;

[NgComponent( IsOptional = true )]
[Package<BackOfficePackage>]
[TypeScriptFile( "generic-form.model.ts", "GenericFormData", "IFormControlConfig", "FormControlConfig", "FormControlType" )]
public sealed class GenericFormComponent : NgComponent
{
}
