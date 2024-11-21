using System;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Models an imported name from a <see cref="ITSImportLine"/>.
/// </summary>
public readonly struct TSImportedName : IEquatable<TSImportedName>
{
    /// <summary>
    /// The name that is exported by the source module.
    /// </summary>
    public readonly string SourceName;

    /// <summary>
    /// The local alias name. Defaults to <see cref="SourceName"/>.
    /// </summary>
    public readonly string LocalName;

    /// <summary>
    /// Gets whether this imported name is aliased (<see cref="SourceName"/> is not the sames
    /// as <see cref="LocalName"/>).
    /// </summary>
    public readonly bool HasAlias => !ReferenceEquals( SourceName, LocalName );

    /// <summary>
    /// Initializes a new imported name with its source name and an alias for the local one.
    /// </summary>
    /// <param name="sourceName">The name that is exported by the source module.</param>
    /// <param name="localName">The local alias name.</param>
    public TSImportedName( string sourceName, string localName )
    {
        SourceName = sourceName;
        LocalName = localName;
    }

    /// <summary>
    /// Initializes a new imported name without aliasing.
    /// </summary>
    /// <param name="sourceName">The name that is exported by the source module.</param>
    /// <param name="localName">The local alias name.</param>
    public TSImportedName( string name )
    {
        SourceName = name;
        LocalName = name;
    }

    /// <summary>
    /// Gets "SourceName as LocalName" or "SourceName".
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return HasAlias ? $"{SourceName} as {LocalName}" : SourceName;
    }

    public override bool Equals( object? obj )
    {
        return obj is TSImportedName name && Equals( name );
    }

    public bool Equals( TSImportedName other )
    {
        return SourceName == other.SourceName &&
               LocalName == other.LocalName;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine( SourceName, LocalName );
    }

    public static bool operator ==( TSImportedName left, TSImportedName right )
    {
        return left.Equals( right );
    }

    public static bool operator !=( TSImportedName left, TSImportedName right )
    {
        return !(left == right);
    }
}
