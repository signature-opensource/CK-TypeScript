The TypeScript configuration of a CKomposable application and the attributes that drive the TypeScript generation (the
engine is CK.TypeScript.Engine).

`TypeScriptAspectConfiguration` and `TypeScriptBinPathAspectConfiguration` are the `<TypeScript>` configurations of a
BinPath, their packages and types, their client applications. The TypeScript packages are `TypeScriptPackage`,
`TypeScriptGroup`, `[TypeScriptPackage]`, `[TypeScriptFile]`, `[TypeScriptImportLibrary]`, `[RegisterTypeScriptType]`,
`[AppStyleImport]`, `[YarnPackageExtension]` and `[PackageResolution]`; `[TypeScriptType]` configures a type, and the
`ClientDirectory` lists the client applications of a host.

See the [README](https://github.com/signature-opensource/CK-TypeScript/blob/stable/CK.TypeScript/README.md).
