# TypeScript source code generator

This assembly introduces the [TypeScriptAspectConfiguration](TypeScriptAspectConfiguration.cs)
and the [TypeScriptAttribute](TypeScriptAttribute.cs).

This is enough to drive the generation: the heavy stuff is in the [CK.StObj.TypeScript.Engine](..\CK.StObj.TypeScript.Engine).

Note that this package depends on the CK.Poco.Json: generating TypesScript requires that the Json serialization is available. 

## Basic types & Code Generator

> Generic code generation from C# to TypeScript is almost an impossible task.

Generators dedicated to specific type (or to a type family) must always be written
so that a `.ts` file eventually contains the TypeScript projection of the C# Type.

Exceptions to this rule exist for very simple types that follows the ECMAScriptStandard serialization
mode (see [CK.Poco.Json](https://github.com/signature-opensource/CK-StObj/tree/master/CK.Poco.Json/README.md)).

There is no need of any `.ts` file for them.
 - Basic types that have direct counterparts in TypeScript: 
   - `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `float`, `double` are mapped to [number](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Number).
   - `long`, `ulong`, `decimal`, `BigInteger` are mapped to [BigInt](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/BigInt).
   - `bool` is mapped to `boolean`.
   - `string` is mapped to `string`.
 - C# `object` are mapped to `unknown`. This is cleaner and safer than `any` (see [here](https://stackoverflow.com/a/51439876/190380)).
   (Note that `{}` or `Object` could have been chosen, but not `object` - see [here](https://stackoverflow.com/a/28795689/190380).)
 - `IDictionary<,>` and `Dictionary<,>` can be mapped to [Map](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Map).
 - `ISet<>` and HashSet<> can be mapped to [Set](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Set).
 - C# arrays, `IList<>` and `List<>` can be mapped to [js array](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Array).
 - Value tuples can use [Tuples](https://www.typescriptlang.org/docs/handbook/variable-declarations.html#tuple-destructuring).

Any other type requires an explicit generation and a final `.ts` file that will contain
(and export) the generated TS type.

There exist however one "generic code generator" that handles enumerations: enums are
simple enough to be unambiguously mapped:

```csharp
/// <summary>
/// Folder for this type is explicitly "TheFolder" at the root of the TS export.
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
 * Folder for this type is explicitly "TheFolder" at the root of the TS export.
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
In the `TheFolder/DemoEnum.ts` file of the export folder.

## The [TypeScript] attribute

A `TypeScriptAttribute` on interfaces, classes or structs configures the generation (target folder, file name and TypeScript type name)
and requests a code generation for it (but does not provide a generator).

Actual generators are either global [ITSCodeGenerator](../CK.StObj.TypeScript.Engine/ITSCodeGenerator.cs) or bound
to a type [ITSCodeGeneratorType](../CK.StObj.TypeScript.Engine/ITSCodeGeneratorType.cs)

`TypeScriptAttribute` is optional: when no attribute is set, a type can be declared as a being a "TypeScript type" thanks to at least
one call to `DeclareTSType` methods of the central [TypeScriptContext](../CK.StObj.TypeScript.Engine/TypeScriptContext.cs) during code generation.

This *optionality* has many advantages:
  - It enables the support of generic generators for some types (based on interfaces, other attributes or any other hints) without the exported types 
    even knowing that they are exported.
  - When set, this gives a simple and standardized/unique way to configure target folder, file name and TypeScript type name.
  - It enables code generators to create/check/alter any *missing attribute* whether they have been defined or not.

## Samples of implementations
   
An example of a global code generator is the `IPoco` generator [PocoCodeGenerator](../CK.StObj.TypeScript.Engine/Poco/PocoCodeGenerator.cs) that
is available by default.
It is not simple and rely on the IPoco discovery mechanism that captures the Poco model (their multiple interfaces with their properties).

An example of a type bound code generator is planned to be exposed [here (in tests)](../Tests/CK.StObj.TypeScript.Tests/CodeGeneratorTypeSample/)
but it's important to note that a generator in real life runs in the "engine space", it's ".Engine" assembly is dynamically resolved
by the CKSetup process and run by the StObjEngine. It would be tedious to setup a repository with its required CKSetup store support
to handle one (or a few) types.

As stated in the introduction, providing a generic code generator for any kind of types (any class) is pure utopia. The answer to
this issue will be in the "Poco Like" family that is under development in the StObjEngine: a standard `PocoLikeCodeGenerator` will
then be able to generate TS for these beasts since their "shape" will be controlled to be simple enough.



