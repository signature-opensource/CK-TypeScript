# TypeScript source code generator

This assembly introduces the [TypeScriptAspectConfiguration](Configuration/TypeScriptAspectConfiguration.cs) and the
BinPath specific [TypeScriptBinPathAspectConfiguration](Configuration/TypeScriptBinPathAspectConfiguration.cs).


[TypeScriptTypeAttribute](TypeScriptTypeAttribute.cs) can decorate any C# type to optionnaly specify its generated
TypeScript type name, target file and folder but this is not required as engines can generate TypeScript code for
any kind of C# object the way they want.

Specialized [TypeScriptPackage](TypeScriptPackage.cs) are used to define TypeScript resources that can be `.ts` files or any
other kind of files (`.less`, `.html`, `.png`, etc.). TypeScript packages, unlike Sql packages, are not
meant to expose methods or any kind of API. They are "resource packages", their sole purpose is to contain resources
(and transformers) and to define the dependency topology thanks to the set of attributes defined in CK.ResourceSpace.Abstractions
package: the most often used ones are `[Requires<T1,...Tn>]`, `[RequiredBy<T1,...Tn>]` that define the requirements and `Package<TOwner>`
that defines the ownership. The `[Children]` and `[Groups]` attributes can also be used. 

Resources are in "Res/" and "Res[After]/" folders. They don't need to be declared on the C# side.
The [TypeScriptFileAttribute](TypeScriptFileAttribute.cs) can be used to declare TypeScript type/symbol name that
are exported by a resource file.

The [TypeScriptImportLibraryAttribute](TypeScriptImportLibraryAttribute.cs) is used to declare dependencies on npm packages.

Two attributes of a package fix the manifests of the libraries it brings, in the generated application:

| Attribute | Writes | Use it to |
|---|---|---|
| [`[YarnPackageExtension( selector, kind, name, range )]`](YarnPackageExtensionAttribute.cs) | the `packageExtensions` of the application's `.yarnrc.yml` | add a dependency or a peer that a package doesn't declare. |
| [`[PackageResolution( pattern, version )]`](PackageResolutionAttribute.cs) | the `resolutions` of the application's `package.json` | redirect a declared dependency to another version. |

For instance, ng-zorro-antd uses rxjs (through @angular/cdk) but doesn't declare it: without the peer, Yarn
Plug'n'Play creates a second instance of its CDK.

```csharp
[YarnPackageExtension( "ng-zorro-antd@*", DependencyKind.PeerDependency, "rxjs", "*", Reason = "..." )]
```

- The selector of an extension must carry a range (`@*` unless a later version fixes the manifest): Yarn rejects a bare
  name and silently ignores a range that matches nothing. The kind is `Dependency` or `PeerDependency` (only the
  `peerDependencies`, unlike `[TypeScriptImportLibrary]`): Yarn has no dev dependencies in an extension.
- A resolution also redirects a dependency added by an extension, never a peer (a peer is provided by the dependent).
- When two packages declare the same entry, two versions are merged like the library versions (the more specific one
  wins); otherwise the last one in the topological order of the packages wins.

This is enough to drive the generation: the heavy stuff is in the [CK.TypeScript.Engine](../CK.TypeScript.Engine).

Note that this package depends on the CK.Poco.Exc.Json but Json serialization may be available or not for each BinPath:
Poco compliant types can be generated with or without serialization support.

## Client applications

A `<TypeScript>` configuration can have a `Name`, from which its other properties derive, and a `Kind` that says
whether it is a client application served by its host:

```xml
<TypeScript Name="App" Kind="DefaultApp" DevServerPort="4200" />
<TypeScript Name="MiniApp" Kind="App" DevServerPort="4201" />
```

| Property | Derived from the `Name` (an explicit value wins) |
|---|---|
| `TargetProjectPath` | `{ProjectPath}/Clients/{Name}` |
| `TypeFilterName` | `TypeScript{Name}` |
| `RequestPath` | `/` for the `DefaultApp`, `/{kebab-name}` for an `App` (`/mini-app`), none for `None` (the default kind) |

The `Name` is an identifier (letters and digits, starting with a letter), unique in its BinPath; a client application
requires one and there is at most one `DefaultApp` per BinPath. Without a `Name`, the defaults are unchanged
(`TypeFilterName` is `"TypeScript"`).

A client application also requires a `DevServerPort`, unique in its BinPath (choose distinct ports for the hosts that
run together): in Development, the host starts its dev server on this port. Its optional `UseDist` makes the host serve
its built files even in Development.

The Angular engine writes the request path of a client application as the `"baseHref"` of its `angular.json`, and its
`DevServerPort` as the port of its `serve` target. The
engine generates the [`ClientDirectory`](ClientDirectory.cs) of the host: its [`ClientInfo`](ClientInfo.cs) list the
client applications (the default one last). The [CK.TS.AspNet.Clients](https://github.com/signature-opensource/CK-TS-AspNet/blob/stable/CK.TS.AspNet.Clients/README.md) package
serves them; the link goes to a package built on this one because it is the consumer of this list.

A client application is built by the BinPath, but it is a client of a party only when the party **declares** it in its
configuration: `"CK-AppIdentity": { "Local": { "PartyClient": { "App": {} } } }` (the "PartyClient" feature of
[CK.AppIdentity.PartyClient](https://github.com/signature-opensource/CK-AppIdentity/blob/stable/CK.AppIdentity.PartyClient/README.md)).
Waiting for a configuration that unifies the build and the deployment, the engine adds the missing declarations of the
root party to the `appsettings.json` of a host project (see [CK.TypeScript.Engine](../CK.TypeScript.Engine/README.md)):
commit them.

## Selecting the packages of a configuration

A BinPath can have multiple `<TypeScript>` configurations (one per target project, each with its `TypeFilterName`).
Each configuration selects its packages among the BinPath's TypeScript packages and groups:

- The packages listed in its `<Types>` are its roots. When no package is listed, the roots are all the packages that
  are not optional (`[TypeScriptPackage( IsOptional = true )]`).
- The packages of its `<ExcludedTypes>` are never selected: a selected package that requires one of them is an error.
- The selected packages are the roots and the packages they reach through their required relations (`[Requires]`,
  `[Children]`, `[Package]`, etc., and the abstractions they reference: see the resource space's mappings). Only the
  relations declared by attributes are considered.

Only the selected packages are handled: their resources and transformers, and the types they register
(`[RegisterTypeScriptType]`) that define the set of Poco types this configuration exchanges. The other packages
are known by the resource space as dropped optional packages: a transformer that targets one of them is ignored.

A type name in `<Types>` or `<ExcludedTypes>` can be a pattern: `"Type, Assembly"` where each part is an exact name or a
prefix followed by a `*` (`"Acme.Front.*, Acme.Front"`, `"*, Acme.Commands"`). A pattern of the `<Types>` doesn't demand
a TypeScript generator for each type it matches (an explicit type does): it selects among the BinPath's types the
exchangeable Poco compliant types (they are registered) and the packages (they are listed), and ignores the other
ones. It can only configure the `Folder` of the registered types. A pattern of the `<ExcludedTypes>` excludes the
matching types; an explicit type of the `<Types>` that it matches is an error.

```xml
<!-- Illustrative names: a small front that only needs one package of the BinPath. -->
<TypeScript TargetProjectPath="{ProjectPath}/Clients/MiniApp" TypeFilterName="TypeScriptMiniApp">
  <Types>
    <!-- A package: the MiniApp gets it and what it requires, nothing else. -->
    <Type Name="Acme.Front.MiniAppPackage, Acme.Front" />
    <!-- A Poco type exchanged by the MiniApp. -->
    <Type Name="Acme.Front.IInfoCommand, Acme.Front" />
    <!-- All the Poco commands of a namespace (and the packages there, if any). -->
    <Type Name="Acme.Front.Mini.*, Acme.Front" />
  </Types>
  <ExcludedTypes>
    <Type Name="Acme.Front.HeavyChartsPackage, Acme.Front" />
    <Type Name="Acme.Front.Experimental.*, Acme.Front" />
  </ExcludedTypes>
</TypeScript>
```

## Basic types & Code Generator

> Generic code generation from C# to TypeScript is almost an impossible task.

Generators dedicated to specific type (or to a type family) must always be written
so that a `.ts` file eventually contains the TypeScript projection of the C# Type except for
basic types.

There is no need of any `.ts` file for the folllowing types:
 - Basic types that have direct counterparts in TypeScript: 
   - `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `float`, `double` are mapped to [number](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Number).
   - `long`, `ulong`, `BigInteger` are mapped to [bigint](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/BigInt).
   - `bool` is mapped to `boolean`.
   - `string` is mapped to `string`.
 - C# `object` is mapped to `{}`: the TypeScript type that contains objects and JavaScript primitive types except `undefined`
   and `null` (unlike `object` - see [here](https://stackoverflow.com/a/28795689/190380)).
 - `IDictionary<,>` and `Dictionary<,>` can be mapped to [Map](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Map).
 - `ISet<>` and HashSet<> can be mapped to [Set](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Set).
 - C# arrays, `IList<>` and `List<>` can be mapped to [js array](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Array).
 - Value tuples can use [Tuples](https://www.typescriptlang.org/docs/handbook/variable-declarations.html#tuple-destructuring).

Some basic types can be supported with the help of wellknown libraries:
 - `TimeSpan`, `DateTime` and `DateTimeOffset` can be mapped to `Duration` and `DateTime` from the
   [luxon](https://github.com/moment/luxon/) library.
 - `Decimal` can be mapped to `Decimal` from [decimal.js-light](https://mikemcl.github.io/decimal.js-light/)
   or from [decimal.js](https://mikemcl.github.io/decimal.js/) libraries.

Any other type requires an explicit generation and a final `.ts` file that will contain
(and export) the generated TS type.

There exist however one "generic code generator" that handles enumerations: enums are
simple enough to be unambiguously mapped:

```csharp
/// <summary>
/// Folder for this type is explicitly "TheFolder" at the root of the /ck-gen folder.
/// </summary>
[TypeScriptType( Folder = "TheFolder" )]
public enum DemoEnum : byte
{
    /// <summary>
    /// Alpha.
    /// </summary>
    Alpha,

    /// <summary>
    /// Beta.
    /// </summary>
    Beta,

    Chi = 3712
}
```
Is automatically generated as:
```ts
/**
 * Folder for this type is explicitly "TheFolder" at the root of the /ck-gen folder.
 **/
export enum DemoEnum {

    /**
     * Alpha.
     **/
    Alpha = 0,

    /**
     * Beta.
     **/
    Beta = 1,

    Chi = 3712
}
```
In the `ck-gen/TheFolder/DemoEnum.ts` file of the target project path.

## The [TypeScriptType] attribute

A `TypeScriptTypeAttribute` on interfaces, classes or structs configures the generation (target folder, file name and TypeScript type name)
and requests a code generation for it (but does not provide a generator).

Actual generators are either global [ITSCodeGenerator](../CK.TypeScript.Engine/ITSCodeGenerator.cs) or bound
to a type [ITSCodeGeneratorType](../CK.TypeScript.Engine/ITSCodeGeneratorType.cs)

`TypeScriptTypeAttribute` is optional: when no attribute is set, a type can be declared as a being a "TypeScript type" thanks to at least
one call to `TSTypeManager.ResolveTSType` (the `Root.TSTypes` of the central [TypeScriptContext](../CK.TypeScript.Engine/TypeScriptContext.cs)) during code generation.

This *optionality* has many advantages:
  - It enables the support of generic generators for some types (based on interfaces, other attributes or any other hints) without the exported types 
    even knowing that they are exported.
  - When set, this gives a simple and standardized/unique way to configure target folder, file name and TypeScript type name.
  - It enables code generators to create/check/alter any *missing attribute* whether they have been defined or not.

## Samples of implementations
   
An example of a global code generator is the `IPoco` generator [PocoCodeGenerator](../CK.TypeScript.Engine/Poco/PocoCodeGenerator.cs) that
is available by default.
It is not simple and rely on the IPoco discovery mechanism that captures the Poco model (their multiple interfaces with their properties).

As stated in the introduction, providing a generic code generator for any kind of types (any class) is pure utopia.
The "generic translation" for Poco relies on the strict definition of the Poco Type System.



