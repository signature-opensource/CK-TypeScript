using CK.Core;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.TypeScript.Engine;

sealed class TypeScriptPackageCollection
{
    readonly ImmutableArray<TypeScriptPackageAttributeImpl> _implPackages;

    /// <summary>
    /// Logical TypeScript package that wraps the <see cref="TypeScriptPackageAttributeImpl"/>
    /// that defines it.
    /// </summary>
    internal sealed class Package
    {
        readonly TypeScriptPackageAttributeImpl _impl;

        internal Package( TypeScriptPackageAttributeImpl impl )
        {
            _impl = impl;
        }
    }



    TypeScriptPackageCollection( ImmutableArray<TypeScriptPackageAttributeImpl> implPackages )
    {
        _implPackages = implPackages;
    }

    public static TypeScriptPackageCollection? Create( IActivityMonitor monitor, ImmutableArray<TypeScriptPackageAttributeImpl> implPackages )
    {


        return new TypeScriptPackageCollection( implPackages );
    }
}
