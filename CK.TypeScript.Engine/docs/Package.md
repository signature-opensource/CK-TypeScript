The TypeScript engine of a CKomposable application: the [`TypeScriptAspect`](https://github.com/signature-opensource/CK-TypeScript/blob/stable/CK.TypeScript.Engine/TypeScriptAspect.cs)
generates, for each TypeScript configuration, the `ck-gen/` folder of the target project (the TypeScript projection of
the C# and Poco types, the resources of the TypeScript packages with their transformers) and integrates it in the project
(`package.json`, `tsconfig.json` paths for `@local/ck-gen`, Yarn, Jest). Extension points: `ITSCodeGeneratorFactory`,
`ITSCodeGenerator`, `ITSCodeGeneratorType`, the `ITSPocoCodeGenerator` events, the `TypeScriptContext` and the
`TypeScriptIntegrationContext` events.
See the [README](https://github.com/signature-opensource/CK-TypeScript/blob/stable/CK.TypeScript.Engine/README.md).
