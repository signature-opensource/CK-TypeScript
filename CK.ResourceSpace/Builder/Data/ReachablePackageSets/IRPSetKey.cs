using System;

namespace CK.Core;

interface IRPSetKey
{
    int Length { get; }

    bool IsLocalDependent { get; }

    ReadOnlySpan<int> PackageIndexes { get; }
}
