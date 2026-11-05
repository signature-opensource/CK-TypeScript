# CK.TypeScript.LiveEngine

A change of a resource of a package (a `.ts`, `.html` or `.less` file, an asset, a locale) should be visible in the
TypeScript application without building the .NET application again. The live engine is a console application that runs
beside the TypeScript application while it is developed: it watches the resources of the local packages and applies
their changes to the generated `ck-gen/` folder as they happen.

## Launching it

The build adds a `ck-watch` script to the `package.json` of the target project, that runs the
`CK.TypeScript.LiveEngine.dll` of the application that ran the engine: `dotnet "$PROJECT_CWD/{relative path}"`. (The
script is written by [CK.TypeScript.Engine](../CK.TypeScript.Engine/README.md), a package built on this one: the link
goes there because it is the one that writes the script.) From the target project:

```
yarn ck-watch
```

The engine references this package only to compute this path: the [MSBuild target](MSBuild/CK.TypeScript.LiveEngine.targets)
of the package (`buildTransitive`) copies the `runtimeconfig.json` and `deps.json` of the project being built as
`CK.TypeScript.LiveEngine.runtimeconfig.json` and `CK.TypeScript.LiveEngine.deps.json`, so that the dll can run from this
output folder. The assemblies are loaded from this folder.

The options, from [Program.cs](Program.cs):

| Option | Effect |
|---|---|
| `-v`, `--verbosity` | The log level: `Q[uiet]`, `M[inimal]`, `N[ormal]` (the default), `D[etailed]` or `Diag[nostic]`. |
| `-d`, `--debounce` | The debounce time of the file changes, in milliseconds: 100 by default, clamped between 40 and 1000. |
| `--debug-launch` | Launches a debugger at startup. |
| `-?`, `-h`, `--help` | Displays the help. |

Ctrl+C stops it.

## What it watches

The live engine works from the current directory (the target project): it reads the Live state that the build wrote in
`ck-gen-app/.ck-watch/LiveState.dat` (see the Live state in
[CK.ResourceSpace](https://github.com/signature-opensource/CK-ResourceSpace/blob/stable/CK.ResourceSpace/README.md#the-live-state-is-written-at-install-time-and-disabled-by-a-string)).
The [`Runner`](Runner.cs) loop:

1. Waits for this file (it is checked every 500 ms) and restores the Live updaters of the resource space handlers from it.
2. Watches the folders of the local packages (their `Res/` and `Res[After]/` resources) and the `ck-gen-app/` folder,
   except its `.ck-watch/` folder. A resource that the code handles (`ResCoreData.CodeHandledResources`) is ignored.
3. Sends each change to the Live updaters, and applies the changes once no change happened for the debounce time.
4. Stops watching when the Live state file changes (a new build deleted it), and goes back to 1.

The Live updaters are those of the handlers: the transformable files (see
[CK.ResourceSpace.Transformable](../CK.ResourceSpace.Transformable/README.md#live)), the assets
(CK.ResourceSpace.Assets) and the locales (CK.ResourceSpace.Globalization).

## Requires.

- [CK.ResourceSpace.Transformable](../CK.ResourceSpace.Transformable/README.md) for the transformable files.
- [CK.ResourceSpace.Assets](https://github.com/signature-opensource/CK-ResourceSpace/blob/stable/CK.ResourceSpace.Assets/README.md)
  and [CK.ResourceSpace.Globalization](https://github.com/signature-opensource/CK-ResourceSpace/blob/stable/CK.ResourceSpace.Globalization/README.md)
  for the assets and the locales.
