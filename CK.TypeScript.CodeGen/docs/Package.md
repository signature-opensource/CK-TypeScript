The model of the generated TypeScript code: a [`TypeScriptRoot`](https://github.com/signature-opensource/CK-TypeScript/blob/stable/CK.TypeScript.CodeGen/TypeScriptRoot.cs)
holds the folders (`TypeScriptFolder`), the files (`TypeScriptFile`, `ResourceTypeScriptFile`) with their code parts
(`ITSCodePart`) and imports (`ITSFileImportSection`), the mapping of C# types to TypeScript types (`TSTypeManager`,
`ITSType`) and the external libraries (`LibraryManager`). It generates the code (`TypeScriptRoot.GenerateCode`) and
publishes the files (`TypeScriptRoot.Publish`). `PackageJsonFile` and `TSConfigJsonFile` model the target project's files.
See the [README](https://github.com/signature-opensource/CK-TypeScript/blob/stable/CK.TypeScript.CodeGen/README.md).
