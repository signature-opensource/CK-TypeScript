using System;
using System.Collections.Immutable;

namespace CK.Core;

/// <summary>
/// Opaque object that supports <see cref="ResPackageDataHandler{T}"/> machinery.
/// </summary>
public interface IResPackageDataCache
{
    internal void LocalOnly();
}

