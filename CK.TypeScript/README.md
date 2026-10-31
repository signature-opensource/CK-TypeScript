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
The [TypeScriptFilesAttribute](TypeScriptFilesAttribute.cs) can be used to declare TypeScript type/symbol name that
are exported by a resource file.

The [TypeScriptImportLibraryAttribute](TypeScriptImportLibraryAttribute.cs) is used to declare dependencies on npm packages.

This is enough to drive the generation: the heavy stuff is in the [CK.TypeScript.Engine](..\CK.TypeScript.Engine).

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
client applications (the default one last). The [CK.TS.AspNet.Clients](../CK.TS.AspNet.Clients/README.md) package
serves them; the link goes to a package built on this one because it is the consumer of this list.

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
 - C# `object` are mapped to `unknown`. This is cleaner and safer than `any` (see [here](https://stackoverflow.com/a/51439876/190380)).
   (Note that `{}` or `Object` could have been chosen, but not `object` - see [here](https://stackoverflow.com/a/28795689/190380).)
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
[TypeScript( Folder = "TheFolder" )]
public enum DemoEnum : byte
{
    /// <summary>
    /// Alpha.
    /// </summary>
    Alpha,

    /// <summary>
    /// Beta.
    /// </summary>
    Beta

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

## The [TypeScript] attribute

A `TypeScriptAttribute` on interfaces, classes or structs configures the generation (target folder, file name and TypeScript type name)
and requests a code generation for it (but does not provide a generator).

Actual generators are either global [ITSCodeGenerator](../CK.StObj.TypeScript.Engine/ITSCodeGenerator.cs) or bound
to a type [ITSCodeGeneratorType](../CK.StObj.TypeScript.Engine/ITSCodeGeneratorType.cs)

`TypeScriptAttribute` is optional: when no attribute is set, a type can be declared as a being a "TypeScript type" thanks to at least
one call to `ResolveTSType` methods of the central [TypeScriptContext](../CK.StObj.TypeScript.Engine/TypeScriptContext.cs) during code generation.

This *optionality* has many advantages:
  - It enables the support of generic generators for some types (based on interfaces, other attributes or any other hints) without the exported types 
    even knowing that they are exported.
  - When set, this gives a simple and standardized/unique way to configure target folder, file name and TypeScript type name.
  - It enables code generators to create/check/alter any *missing attribute* whether they have been defined or not.

## Samples of implementations
   
An example of a global code generator is the `IPoco` generator [PocoCodeGenerator](../CK.StObj.TypeScript.Engine/Globals/PocoCodeGenerator.cs) that
is available by default.
It is not simple and rely on the IPoco discovery mechanism that captures the Poco model (their multiple interfaces with their properties).

As stated in the introduction, providing a generic code generator for any kind of types (any class) is pure utopia.
The "generic translation" for Poco relies on the strict definition of the Poco Type System.



