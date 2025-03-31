using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Core;

sealed class OneBasedArray : IReadOnlyList<ResPackage>
{
    readonly ImmutableArray<ResPackage> _packages;

    public OneBasedArray( ImmutableArray<ResPackage> packages )
    {
        _packages = packages;
    }

    public ResPackage this[int index] => _packages[index - 1];

    public int Count => _packages.Length - 1;

    public IEnumerator<ResPackage> GetEnumerator() => _packages.Skip( 1 ).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

