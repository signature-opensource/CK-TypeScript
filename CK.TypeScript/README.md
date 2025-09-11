# TypeScript source code generator

This assembly introduces the [TypeScriptAspectConfiguration](TypeScriptAspectConfiguration.cs) and the
BinPath specific [TypeScriptBinPathAspectConfiguration](TypeScriptBinPathAspectConfiguration.cs).


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



