# CK.ResourceSpace.Transformable

In a resource space, a package can modify the resources of the packages it reaches without copying them: it contains
transformers (`.t` files) whose transform functions apply to a resource of another package (a `.ts`, `.html` or `.less`
file, depending on the languages) or to another transform function. This package is the resource space handler that
collects these resources, applies the transform functions and installs the final texts, at build time and live.

The resource space itself (packages, reachability, handlers and installers) is described by
[CK.ResourceSpace](https://github.com/signature-opensource/CK-ResourceSpace/blob/stable/CK.ResourceSpace/README.md); the
transformer languages by [CK.Transform.Core](https://github.com/signature-opensource/CK-Transform/blob/stable/CK.Transform.Core/README.md).
The public types are in the `CK.Core` namespace.

## The handler

[`TransformableFileHandler`](TransformableFileHandler.cs) is a `ResourceSpaceFileHandler`:

```csharp
public TransformableFileHandler( IResourceSpaceItemInstaller? installer,
                                 TransformerHost transformerHost,
                                 IExternalTransformableItemResolver? externalItemResolver,
                                 params ImmutableArray<TransformableFileInstallHook> installHooks )
```

It handles the file extensions of the languages of the `TransformerHost`. When the space is built, every resource of
these languages is registered:

- A resource of the transform language is a source of transform functions. Its source name is its file name without
  the `.t` suffix and without an optional language hint: a `.ts.t` file can only contain TypeScript transformers.
- Any other resource is an item to install. Its target path is the package's `DefaultTargetPath` combined with the
  resource name: two resources with the same target path are an error.

At install time, each item gets its final text (its text transformed by its transform functions, in order) and goes to
the installer.

## How a transform function finds its target

A transform function targets an item of a package that its package reaches (`IResPackageResources.Reachables`), in the
same language (see [`TransformEnvironment.TargetFinder`](TransformEnvironment.TargetFinder.cs)):

- Without a target, the source name is a prefix of the target name: a `top-bar.t` finds a `top-bar.component.ts`.
- A target with a sub path (`Ng/AXIOSToken`) uses it as the expected path of the item, to disambiguate.
- A target name with a file extension is an exact name, otherwise it is a prefix.

No candidate, or more than one, is an error, unless the target matches a resource path of an optional package that the
space excluded (`ResCoreData.ExcludedOptionalResourcePaths`): the transformer is then ignored.

A transformer of transformer (a function of the transform language) targets another transform function by its name:
the function's own name, or `{package full name}({target name})`. The targeted function must be in a reachable package,
and two functions with the same name are an error.

## External items

A target that starts with `../` is outside the resource space. During the build, it is resolved by the
[`IExternalTransformableItemResolver`](ExternalItem/IExternalTransformableItemResolver.cs) given to the handler (no
resolver is an error); in Live, such a transformer is ignored with a warning. The resolver returns an
[`ExternalTransformableItem`](ExternalItem/ExternalTransformableItem.cs) (its `InitialText` and `ExternalPath`) whose
`Install` method is called with the transformed text, only when the transformation changed it. The external items are
handled once the regular items are installed.

## Install hooks

The hooks see each item before the installer: [`ITransformableFileInstallHook`](FileInstallHook/ITransformableFileInstallHook.cs)
has a `StartInstall`, a `HandleInstall` per item (that can change the final text and say that it handled the item) and
a `StopInstall`. The hooks are called in order until one handles the item; an item that no hook handles is written by the
installer at its target path.

A hook given to the handler is a [`TransformableFileInstallHook`](FileInstallHook/TransformableFileInstallHook.cs): it
has access to the `SpaceData` and the `TransformerHost`, and it writes its Live state (`WriteLiveState`, unless
`ShouldWriteLiveState` is false). Its type must then define the static method that restores its Live side:

```csharp
public static ILiveTransformableFileInstallHook? ReadLiveState( IActivityMonitor monitor,
                                                                ResCoreData spaceData,
                                                                IBinaryDeserializer d );
```

An [`ILiveTransformableFileInstallHook`](FileInstallHook/ILiveTransformableFileInstallHook.cs) also handles the removed
items (`HandleRemove`): an item that no hook handles is deleted by the installer.

## Live

`TransformableFileHandler` is an `ILiveResourceSpaceHandler` (see the Live state in
[CK.ResourceSpace](https://github.com/signature-opensource/CK-ResourceSpace/blob/stable/CK.ResourceSpace/README.md#the-live-state-is-written-at-install-time-and-disabled-by-a-string)).
Its Live update is disabled unless its installer is a `FileSystemInstaller`. Its Live state holds the installer's
target path, the hooks, the types of the transform languages (each one needs a public default constructor) and the
items and functions. `TransformableFileHandler.ReadLiveState` restores the Live updater, which:

- tracks the changes of the local files of the handled languages, and of the folders: a folder that appears (renamed,
  moved, the package's folder restored) brings its files, outside the root folders of the folder handlers;
- applies them: an item whose text changed is installed again with its transform functions, a local item that
  disappeared is removed, and a changed source of transform functions updates the items they target;
- keeps the functions that lost their target, and binds them again to a new item at the same target path, or to the
  new item that the initial binding would find (by name, in the reachable packages, when it is the only candidate).

## Requires.

- [CK.ResourceSpace](https://github.com/signature-opensource/CK-ResourceSpace/blob/stable/CK.ResourceSpace/README.md)
  for the resource space, its handlers and its Live state.
- [CK.Transform.Core](https://github.com/signature-opensource/CK-Transform/blob/stable/CK.Transform.Core/README.md) for
  the `TransformerHost` and the transform functions.
- [CK.BinarySerialization.Sliced](https://github.com/signature-opensource/CK-BinarySerialization/blob/stable/CK.BinarySerialization.Sliced/README.md)
  for the Live state.
