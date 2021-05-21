# TypeScript source code generator

This assembly introduces the [TypeScriptAspectConfiguration](TypeScriptAspectConfiguration.cs)
and the [TypeScriptAttribute](TypeScriptAttribute.cs).

This is enough to drive the generation: the heavy stuff is in the [CK.StObj.TypeScript.Engine](..\CK.StObj.TypeScript.Engine).

## Basic types & Code Generator

> Generic code generation from C# to TypeScript is almost an impossible task.

Generators dedicated to specific type (or to a type family) must always be written
so that a `.ts` file eventually contains the TypeScript projection of the C# Type.

Exceptions to this rule exist for very simple types that somehow already exist:
no need of any `.ts` file for them.
 - Basic types that have direct counterparts in TypeScript: `int`, `float`, `double`, `bool` and `string`.
 - `object` that can be mapped to `Unknown` (or possibly to `any'?).
 - `IDictionary<,>` and `Dictionary<,>` can be mapped to `Map`.
 - `ISet<>` and HashSet<> can be mapped to `Set`.
 - `IList<>` and `List<>` can be mapped to js array.
 - Value tuples can use js arrays.

Any other type requires an explicit generation and a final `.ts` file that will contain
(and export) the generated TS type.

There exist however one "generic code generator" that handles enumerations: enums are
simple enough to be unambiguously mapped:

```csharp
/// <summary>
/// Folder is explicitly "TheFolder".
/// </summary>
[TypeScript( Folder = "TheFolder" )]
public enum InAnotherFolder : byte
{
    /// <summary>
    /// Alpha.
    /// </summary>
    Alpha,

    /// <summary>
    /// Beta.
    /// </summary>
    Beta
}
```
Is automatically generated as:
```ts
/**
 * Folder is explicitly "TheFolder".
 **/
export enum InAnotherFolder {

    /**
     * Alpha.
     **/
    Alpha = 0,

    /**
     * Beta.
     **/
    Beta = 1
}
```
A `TypeScriptAttribute` on interfaces, classes or structs configures the generation (target folder, file name and TypeScript type name)
and requests a code generation for it (but does not provide a generator).

Actual generators are either global [ITSCodeGenerator](../CK.StObj.TypeScript.Engine/ITSCodeGenerator.cs) or bound
to a type [ITSCodeGeneratorType](../CK.StObj.TypeScript.Engine/ITSCodeGeneratorType.cs)

`TypeScriptAttribute` is optional: types can be declared as a being TypeScript types thanks to the
`DeclareTSType` methods of the central [TypeScriptContext](../CK.StObj.TypeScript.Engine/TypeScriptContext.cs).
This enables code generators to create/check/alter any *missing attribute*.

An example of a global code generator is the `IPoco` generator [TSIPocoCodeGenerator](../CK.StObj.TypeScript.Engine/Poco/TSIPocoCodeGenerator.cs).
An example of a type bound code generator is [here (in tests)](../Tests/CK.StObj.TypeScript.Tests/CodeGeneratorTypeSample/).


