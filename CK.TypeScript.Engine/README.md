# CK.TypeScript.Engine

The TypeScript engine. When a CKomposable application is built, it generates for each `<TypeScript>` configuration the
`ck-gen/` folder of the target TypeScript project: the TypeScript projection of the C# types (the Poco types among them) and
the resources of the TypeScript packages, transformed by their transformers. It then integrates this code in the target
project: its `package.json` and `tsconfig.json`, and Yarn.

The configuration, the packages and the attributes that drive it are in [CK.TypeScript](../CK.TypeScript/README.md);
the model of the generated code is [CK.TypeScript.CodeGen](../CK.TypeScript.CodeGen/README.md).

## The aspect

[`TypeScriptAspect`](TypeScriptAspect.cs) is the engine aspect of the `TypeScriptAspectConfiguration`. It handles every
`TypeScriptBinPathAspectConfiguration` of the BinPaths:

- A configuration without `TargetProjectPath` targets `{ProjectPath}/Clients/{Name}` when it has a `Name` (and its
  BinPath a `ProjectPath`), otherwise `{ProjectPath}/Client` in the first BinPath; elsewhere, it is removed with a
  warning. A relative path is relative to
  the `BasePath` of the engine configuration, and a trailing `ck-gen` is removed.
- The configurations are checked (see the rules in [CK.TypeScript](../CK.TypeScript/README.md#client-applications)): a
  unique `TargetProjectPath`, a `TypeFilterName` that is `"None"` or starts with `"TypeScript"` and is unique in its
  BinPath, and the `Name`, `RequestPath` and `DevServerPort` of the client applications.
- The generation waits for the Poco type system, and for the Json serialization when CK.Poco.Exc.Json is in the
  application: the `TypeFilterName` of a configuration (unless `"None"`) is then registered as an exchangeable runtime
  filter of its set of Poco types.
- The TypeScript code is generated in the final step of the C# code generation, then saved, or at the end of the run
  when `TypeScriptAspectConfiguration.DeferFileSave` is true.

`CKomposableAppBuilderExtensions.EnsureDefaultTypeScriptAspectConfiguration` ensures that the aspect and a BinPath
configuration exist in a builder.

The [`ClientDirectoryImpl`](ClientDirectoryImpl.cs) generates the `ClientDirectory` of the host from the configurations
that are client applications.

## The TypeScript context

A [`TypeScriptContext`](TypeScriptContext.cs) runs the generation of one configuration. It exposes the `Root`
(`TypeScriptRoot`), the `PocoCodeGenerator`, the `BinPathConfiguration`, the `ActiveCultures`, the `GlobalGenerators`,
the `IntegrationContext` (null when the `IntegrationMode` is `None`) and the `BeforeCodeGeneration`,
`AfterDeferredCodeGeneration` and `AfterCodeGeneration` events (relayed from the `TypeScriptRoot` with the context as
sender).

`TypeScriptContext.Run` builds the resource space of the selected TypeScript packages, resolves the registered types,
generates the code and installs the resources in `{TargetProjectPath}/ck-gen`. The application's own resources are in
`{TargetProjectPath}/ck-gen-app`.

## Code generators

The code generators are found among the implementations of the attributes of the types:

| Extension point | Role |
|---|---|
| [`ITSCodeGeneratorFactory`](ITSCodeGeneratorFactory.cs) | Creates a global `ITSCodeGenerator` (`ITSCodeGeneratorFactory.CreateTypeScriptGenerator`), and can register the C# types that need a TypeScript projection (`ITypeScriptContextInitializer.EnsureRegister`). It can return `ITSCodeGenerator.Empty`. |
| [`ITSCodeGenerator`](ITSCodeGenerator.cs) | A global generator: `StartCodeGeneration` is called once the context is set up (to generate code and subscribe to events), `OnResolveType` and `OnResolveObjectKey` for each type or key to resolve. |
| [`ITSCodeGeneratorType`](ITSCodeGeneratorType.cs) | A generator bound to the type it decorates: `ConfigureBuilder` configures the `RequireTSFromTypeEventArgs` of the type. It must be stateless (it is called once per context). It can specialize `TypeScriptTypeAttributeImpl`. |
| [`ITSPocoCodeGenerator`](ITSPocoCodeGenerator.cs) | The generator of the Poco types (`TypeScriptContext.PocoCodeGenerator`): its `TypeScriptSet`, its `CTSTypeSystem` when Json serialization is available, and the `AbstractPocoGenerating`, `NamedRecordPocoGenerating` and `PrimaryPocoGenerating` events to extend the generated code. |

To resolve a C# type, the context applies its configuration (its `[TypeScriptType]` or the configured one), then calls
the global generators and the generators bound to the type.

## TypeScript packages and their resources

[`TypeScriptGroupOrPackageAttributeImpl`](AttributesImpl/TypeScriptGroupOrPackageAttributeImpl.cs) implements
`[TypeScriptPackage]` and `[TypeScriptGroup]`, and is the base class of the implementations of their specializations: it
creates the package of the resource space (`CreateResPackageDescriptor`, `OnCreateResPackageDescriptor`) and is called
when its package is available (`OnResPackageAvailable`) and when the resource space is (`OnResSpaceAvailable`).
`HasResPackage` false means no package in the resource space. The other attributes of a package derive from
[`TypeScriptGroupOrPackageAttributeImplExtension`](AttributesImpl/TypeScriptGroupOrPackageAttributeImplExtension.cs), like
[`TypeScriptFileAttributeImpl`](AttributesImpl/TypeScriptFileAttributeImpl.cs),
[`AppStyleImportAttributeImpl`](AttributesImpl/AppStyleImportAttributeImpl.cs),
[`YarnPackageExtensionAttributeImpl`](AttributesImpl/YarnPackageExtensionAttributeImpl.cs) and
[`PackageResolutionAttributeImpl`](AttributesImpl/PackageResolutionAttributeImpl.cs) (the invalid declarations are
generation errors that name the decorated type).

The resource space installs in `ck-gen/`:

- the assets (in `ts-assets`) and the locales (in `ts-locales`) of the packages;
- the `.ts`, `.html` and `.less` files, transformed by their transformers (see
  [CK.ResourceSpace.Transformable](../CK.ResourceSpace.Transformable/README.md)), with the TypeScript, Html and Less
  languages of [CK-Transform](https://github.com/signature-opensource/CK-Transform). The install hooks of the context:
  - lift the `variables.less` (or `{folder}.variables.less`) of the packages in `ck-gen/styles/variables.less`;
  - write `ck-gen/styles/styles.less`, that imports the primary `.less` file of each package (`styles.less` or a name
    that starts with the package's folder name), and ensure that the application's `src/styles.less` imports it and the
    `[AppStyleImport]` stylesheets;
  - rewrite the named imports from `'@local/ck-gen'` of a resource `.ts` file to the file that declares each type.

A transformer whose target starts with `../` transforms a file of the target project: it must be in its `src/` folder.

### The generated `locales.ts`

Next to the translation files of the active cultures, `ck-gen/ts-locales/locales.ts` exports `loadTranslations`, the
`LocaleInfo` type, `CKLocales`, the `locales` of the active cultures and `DEFAULT_LOCALE_INFO`. A package adds its
feature to it (a framework's locale data, a translation library, a component library's cultures) with a
[`TSLocalesContributor`](TSLocalesContributor.cs) registered by `TypeScriptContext.AddLocalesContributor`, typically
from the `OnResPackageAvailable` of one of its attribute implementations (an engine registers its own without
package, from `ITSCodeGenerator.StartCodeGeneration`: it comes first). A contributor adds import lines, a prelude
written after the imports, fields of `LocaleInfo` (`TSLocaleInfoField`: a name, a TypeScript type and the value of each
culture) and an epilogue written after `DEFAULT_LOCALE_INFO`; the contributions are written in the order of their
packages (the contributions of an engine first).

## Integration in the target project

When the `IntegrationMode` is `Inline` (the default), the [`TypeScriptIntegrationContext`](TypeScriptIntegrationContext.cs):

- maps `@local/ck-gen` and `@local/ck-gen/*` to the `ck-gen/` folder in the `compilerOptions.paths` of `tsconfig.json`
  (a default `tsconfig.json` is created when there is none);
- updates the dependencies of `package.json` with the libraries of the generated code, and settles the TypeScript
  version, the first one of: the configured library version, the target project's, the one declared by the code, the
  Yarn TypeScript sdk's, the configuration's `DefaultTypeScriptVersion`, `TypeScriptIntegrationContext.DefaultTypeScriptVersion`;
- writes the Yarn settings declared by the packages, merged in their topological order by
  [`YarnSettings`](YarnSettings.cs): the `[PackageResolution]` in the `resolutions` of `package.json` (the declared value
  replaces the entry's, the other entries are kept) and the `[YarnPackageExtension]` in the `packageExtensions` setting of
  the application's `.yarnrc.yml`, each entry preceded by a comment naming its declaring type and its reason. The engine
  owns this setting only: the other settings of the file (Yarn writes some when it migrates an older lockfile) are kept,
  and the file is deleted when nothing else remains;
- runs the Yarn install when needed: Yarn never detects a change of its configuration, so after a successful install
  the engine writes `.yarn/ck-install-state`, the hash of the `package.json` and `.yarnrc.yml` texts, and installs again
  when they don't match it (the marker is deleted before an install: a failed one leaves none). The marker describes
  the local install: the yarn root's `.gitignore` ignores it (`**/.yarn/ck-install-state`);
- after an install, warns when a package of the application's runtime graph (reached from its `dependencies`, at any
  depth) is installed as more than one instance by Yarn Plug'n'Play: a library doesn't declare a peer, and the
  application runs two copies of the package. The warning names the instances and the packages that use them; the fix
  is a `[YarnPackageExtension]`. [`YarnPnpDuplicatedInstances`](YarnPnpDuplicatedInstances.cs) explains its design
  (its cost, why the runtime graph only, why a warning);
- raises `OnBeforeIntegration` (its arguments can add dependencies and run the Yarn install) and `OnAfterIntegration`;
  both can replace the `JestSetup` handler;
- with `AutoInstallJest`, adds Jest to the dev dependencies and a `"test": "jest"` script, and its `JestSetupHandler`
  writes `jest.config.js`, `jest-setup.ts` and a sample `.spec.ts` in `src/` when there is none.

It also adds the `ck-watch` script to `package.json`: `yarn ck-watch` runs the
[CK.TypeScript.LiveEngine](../CK.TypeScript.LiveEngine/README.md) from the target project.

`TypeScriptIntegrationContext.RunYarn`, `TypeScriptIntegrationContext.RunYarnProcess` and
`TypeScriptIntegrationContext.RunProcess` run commands in the target project.

## Yarn

The engine embeds Yarn `4.18.1` (`YarnHelper.AutomaticYarnVersion`) and looks for a Yarn in the target project or above.
The `InstallYarn` option of the configuration decides what happens:

| `YarnInstallOption` | Behavior |
|---|---|
| `AutoUpgrade` (default) | Installs the embedded Yarn when none is found, and upgrades a lower version. |
| `AutoInstall` | Installs the embedded Yarn when none is found. |
| `None` | Never installs Yarn. |

A Yarn is installed at the root of the git working tree (shared by its projects), or in the target project when there is
no `.git` above. The `packageManager` of `package.json` is set to its version. When the Yarn TypeScript sdk doesn't match
the selected TypeScript version, `@yarnpkg/sdks` is added and `yarn sdks vscode` is run. The installs of parallel
processes are serialized by a machine-wide lock on the Yarn root (`TypeScriptIntegrationContext.AcquireYarnRootLock`).

## Requires.

- [CK.TypeScript](../CK.TypeScript/README.md) for the configuration and the attributes.
- [CK.TypeScript.CodeGen](../CK.TypeScript.CodeGen/README.md) for the model of the generated code.
- [CK.TypeScript.LiveEngine](../CK.TypeScript.LiveEngine/README.md): not used by the engine, it is referenced to compute
  the path of the `ck-watch` script.
- [CK.Html.Transform](https://github.com/signature-opensource/CK-Transform/blob/stable/CK.Html.Transform/README.md),
  [CK.Less.Transform](https://github.com/signature-opensource/CK-Transform/blob/stable/CK.Less.Transform/README.md) and
  [CK.TypeScript.Transform](https://github.com/signature-opensource/CK-Transform/blob/stable/CK.TypeScript.Transform/README.md)
  for the transformer languages.
- CK.Poco.Exc.Json.Engine for the Poco types and their Json serialization.
