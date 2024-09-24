using CK.Core;
using System;
using System.IO;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Unifies <see cref="ResourceTextFileBase"/> and <see cref="ResourceUnknownFile"/>.
/// </summary>
public interface IResourceFile
{
    /// <summary>
    /// Gets the resource locator of this file.
    /// </summary>
    ResourceTypeLocator Locator { get; }
}
