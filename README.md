# CK-TypeScript

The TypeScript engine: the generated `@local/ck-gen` code of a CKomposable application, its integration in a
TypeScript application, and the tools to test it and to see the changes of the resources live.

| Package | Description | Latest stable |
|---------|-------------|---------------|
| [CK.TypeScript](CK.TypeScript/README.md) | The TypeScript configuration of the engine (`TypeScriptAspectConfiguration`) and the attributes that drive the generation of a C# type. | [![nuget](https://img.shields.io/nuget/v/CK.TypeScript.svg?label=CK.TypeScript)](https://www.nuget.org/packages/CK.TypeScript/) |
| [CK.TypeScript.CodeGen](CK.TypeScript.CodeGen) | The model of the generated code: the TypeScript files, their code parts and their imports. | [![nuget](https://img.shields.io/nuget/v/CK.TypeScript.CodeGen.svg?label=CK.TypeScript.CodeGen)](https://www.nuget.org/packages/CK.TypeScript.CodeGen/) |
| [CK.TypeScript.Engine](CK.TypeScript.Engine) | The engine: the TypeScript code generators, and the integration of the generated code in the target application (Yarn included). | [![nuget](https://img.shields.io/nuget/v/CK.TypeScript.Engine.svg?label=CK.TypeScript.Engine)](https://www.nuget.org/packages/CK.TypeScript.Engine/) |
| [CK.TypeScript.LiveEngine](CK.TypeScript.LiveEngine) | The live engine: a console application that watches the resources of the packages and applies their changes to the generated application while it runs. | [![nuget](https://img.shields.io/nuget/v/CK.TypeScript.LiveEngine.svg?label=CK.TypeScript.LiveEngine)](https://www.nuget.org/packages/CK.TypeScript.LiveEngine/) |
| [CK.ResourceSpace.Transformable](CK.ResourceSpace.Transformable) | The transformable resources of a resource space: the transform functions and the items they apply to. | [![nuget](https://img.shields.io/nuget/v/CK.ResourceSpace.Transformable.svg?label=CK.ResourceSpace.Transformable)](https://www.nuget.org/packages/CK.ResourceSpace.Transformable/) |
| [CK.IO.Core](CK.IO.Core) | Serializable data: the representation of an exception (`ICKExceptionData`) and the log levels (`LogLevel`, Debug to Fatal). | [![nuget](https://img.shields.io/nuget/v/CK.IO.Core.svg?label=CK.IO.Core)](https://www.nuget.org/packages/CK.IO.Core/) |
| [CK.Testing.TypeScriptEngine](CK.Testing.TypeScriptEngine/README.md) | Tests of the generated TypeScript: runs the engine on a test application and its tests (Jest by default). | [![nuget](https://img.shields.io/nuget/v/CK.Testing.TypeScriptEngine.svg?label=CK.Testing.TypeScriptEngine)](https://www.nuget.org/packages/CK.Testing.TypeScriptEngine/) |

The transformer languages are in [CK-Transform](https://github.com/signature-opensource/CK-Transform), the ASP.NET
support in [CK-TS-AspNet](https://github.com/signature-opensource/CK-TS-AspNet) and the Angular support in
[CK-TS-Angular](https://github.com/signature-opensource/CK-TS-Angular).
