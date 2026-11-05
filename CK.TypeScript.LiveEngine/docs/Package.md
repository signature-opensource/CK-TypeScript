The live engine of the TypeScript applications: a console application, launched from the target project by
`yarn ck-watch` (the script written by CK.TypeScript.Engine), that reads the Live state of the build
(`ck-gen-app/.ck-watch/LiveState.dat`), watches the resources of the local packages and the `ck-gen-app/` folder, and
applies their changes to `ck-gen/` (transformable files, assets, locales) as they happen. Its options: `--verbosity`,
`--debounce`, `--debug-launch`. The package's `buildTransitive` target makes `CK.TypeScript.LiveEngine.dll` runnable from
the output folder of the project that references it.
See the [README](https://github.com/signature-opensource/CK-TypeScript/blob/stable/CK.TypeScript.LiveEngine/README.md).
