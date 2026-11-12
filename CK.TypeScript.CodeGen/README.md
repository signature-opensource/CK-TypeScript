# CK.TypeScript.CodeGen

The model of the generated TypeScript code. The code generators don't write files: they build, in memory, folders,
files, code parts and imports, and map C# types to TypeScript types. Once the code is generated, the whole model is
published to a target. This package knows nothing about the engine that drives it.

The types are in the `CK.TypeScript.CodeGen` namespace, except the models of the target project's files
(`PackageJsonFile`, `TSConfigJsonFile`) that are in `CK.Setup`.

## The root

[`TypeScriptRoot`](TypeScriptRoot.cs) is the central object. Its constructor takes the configured library versions and
the options (`pascalCase`, `generateDocumentation`, `ignoreVersionsBound`, an optional `memory`, `reflectTS` and the
`decimalLibraryName`, `"decimal.js-light"` by default). It exposes:

| Member | Role |
|---|---|
| `Root` | The root `TypeScriptFolder`. |
| `TSTypes` | The `TSTypeManager` that maps C# types (or any key) to `ITSType`. |
| `LibraryManager` | The external libraries the generated code depends on. |
| `DocBuilder` | The `DocumentationBuilder` (always available, even when no documentation is generated). |
| `BeforeCodeGeneration`, `AfterDeferredCodeGeneration`, `AfterCodeGeneration` | The events raised by `GenerateCode`. |
| `GenerateCode` | Raises `BeforeCodeGeneration`, runs the deferred implementors of the types, raises `AfterDeferredCodeGeneration`, locks the type manager and raises `AfterCodeGeneration`. |
| `Publish` | Publishes all the files to an `ITypeScriptPublishTarget`. |

## Folders and files

A [`TypeScriptFolder`](TypeScriptFolder.cs) creates its sub folders (`TypeScriptFolder.FindOrCreateFolder`) and its files:

- A [`TypeScriptFile`](Files/TypeScriptFile.cs) (`TypeScriptFolder.FindOrCreateTypeScriptFile`) is fully generated: it
  has an `Imports` section and a `Body` section, and can declare types (`TypeScriptFile.CreateType`,
  `TypeScriptFile.CreateCSharpType`).
- A [`ResourceTypeScriptFile`](Files/ResourceTypeScriptFile.cs) (`TypeScriptFolder.FindOrCreateResourceFile`) comes from
  a resource: its text is not under the control of the generator, only the types it exports are declared, so that they
  can be resolved and imported.

`TypeScriptFolder.EnsureBarrel` asks for an `index.ts` barrel: it is generated when a file of the folder or below
exports a symbol. A manual `index.ts` file in a folder without barrel is published with a warning: it must export what
the folder exports.

## Code parts

The code is written in parts: an [`ITSCodeWriter`](ITSCodeWriter.cs) collects code, an [`ITSCodePart`](ITSCodePart.cs)
can have sub parts, a `Closer` and a `Memory`, and an [`ITSKeyedCodePart`](ITSKeyedCodePart.cs) is identified by a key.
A part doesn't expose its file: the generators keep the relation with the `TypeScriptFile` explicit.
`TypeScriptFile.CreateDetachedPart` creates an independent part. The fluent `TSCodeWriterExtensions.Append` methods
write the code.

The [`ITSFileImportSection`](Import/ITSFileImportSection.cs) of a file manages its imports: `ImportFromLibrary`,
`ImportFromFile`, `ImportFromLocalCKGen` (from the generated code), `Import` (an `ITSType`) and `EnsureImport` (a C# type).

## Types

An [`ITSType`](TSType/ITSType.cs) is a TypeScript type that can correspond to one, several or no C# type.
[`TSTypeManager`](TSType/TSTypeManager.cs) maps a key (a C# `Type` or any object) to its `ITSType`:

- `TSTypeManager.ResolveTSType` returns the type of a key. An unknown key raises `TSFromTypeRequired` (a C# type) or
  `TSFromObjectRequired` (another object): the [`RequireTSFromTypeEventArgs`](TSType/RequireTSFromTypeEventArgs.cs) is a
  builder that captures the type name, its file and folder, its default value and the `Implementor` that will generate
  its code.
- `object` is mapped to `{}`. `TSTypeManager.RegisterStandardTypes` registers the other basic types: `string` and
  `boolean` always, and depending on its parameters the numbers, the big integers, `decimal` (the `Decimal` type of the
  decimal library) and the Luxon `DateTime` and `Duration` types.

## Libraries and dependencies

The [`LibraryManager`](LibraryManager.cs) registers the [`LibraryImport`](LibraryImport.cs) of the external libraries
(`LibraryManager.RegisterLibrary`). The version of a library is the configured one, otherwise the bound that satisfies
the versions that the code asks for: incompatible bounds are an error, unless `LibraryManager.IgnoreVersionsBound` is
true (the greatest base version then wins). `LibraryManager.ExportDependencyCollection` exports them as a
[`DependencyCollection`](DependencyCollection.cs) of [`PackageDependency`](PackageDependency.cs).

[`PackageJsonFile`](Integration/PackageJsonFile.cs) and [`TSConfigJsonFile`](Integration/TSConfigJsonFile.cs) model
the `package.json` and `tsconfig.json` files of the target project.

## Documentation

The [`DocumentationBuilder`](Comment/DocumentationBuilder.cs) generates the TypeScript comments from the XML documentation
of the C# types (`TSCodeWriterDocumentationExtensions`), read by the `XmlDocumentationReader` of
[CK.CodeGen](https://github.com/signature-opensource/CK-CodeGen/blob/stable/CK.CodeGen/README.md#xml-documentation). The
limitations of the XML documentation files are in the [Comment folder](Comment/README.md).

## Publishing

`TypeScriptRoot.Publish` writes the files to an [`ITypeScriptPublishTarget`](Integration/ITypeScriptPublishTarget.cs).
The [`CodeGenResourceContainerTarget`](Integration/CodeGenResourceContainerTarget.cs) builds a `CodeGenResourceContainer`
(CK.EmbeddedResources) with them.

## Requires.

- [CK.EmbeddedResources](https://github.com/signature-opensource/CK-EmbeddedResources/blob/stable/CK.EmbeddedResources/README.md)
  for the resources and the `CodeGenResourceContainer`.
- [CK.CodeGen](https://github.com/signature-opensource/CK-CodeGen/blob/stable/CK.CodeGen/README.md) for the
  `XmlDocumentationReader`.
- CSemVer for the versions of the libraries (`SVersionBound`).
