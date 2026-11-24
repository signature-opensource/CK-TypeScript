The transformable resources of a resource space (CK.ResourceSpace): the transform functions of the `.t` files are
applied to the resources of the packages they reach, at build time and live.

The [`TransformableFileHandler`](https://github.com/signature-opensource/CK-TypeScript/blob/stable/CK.ResourceSpace.Transformable/TransformableFileHandler.cs)
collects the resources of the languages of a `TransformerHost` (CK.Transform.Core), applies the transform functions of
the `.t` files to the items of the packages they reach, and installs the final texts.
Extension points: the install hooks (`ITransformableFileInstallHook`, `TransformableFileInstallHook`,
`ILiveTransformableFileInstallHook`) and the resolver of items outside the space (`IExternalTransformableItemResolver`).

See the [README](https://github.com/signature-opensource/CK-TypeScript/blob/stable/CK.ResourceSpace.Transformable/README.md).
