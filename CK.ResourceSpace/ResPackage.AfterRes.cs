using CK.BinarySerialization;
using CK.EmbeddedResources;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace CK.Core;

public sealed partial class ResPackage
{
    [SerializationVersion( 0 )]
    sealed class AfterRes : IResPackageResources, ICKSlicedSerializable
    {
        readonly ResPackage _package;
        readonly IResourceContainer _resources;
        readonly string? _localPath;
        readonly int _index;

        public AfterRes( ResPackage package, IResourceContainer resources, int index )
        {
            _package = package;
            _resources = resources;
            _index = index;
            _localPath = resources is StoreContainer c
                        ? c.LocalPath
                        : null;
        }

        [EditorBrowsable( EditorBrowsableState.Never )]
        public AfterRes( IBinaryDeserializer d, ITypeReadInfo info )
        {
            _package = d.ReadObject<ResPackage>();
            _resources = d.ReadObject<IResourceContainer>();
            _index = d.Reader.ReadNonNegativeSmallInt32();
            _localPath = _resources is StoreContainer c
                      ? c.LocalPath
                      : null;
        }

        [EditorBrowsable( EditorBrowsableState.Never )]
        public static void Write( IBinarySerializer s, in AfterRes o )
        {
            s.WriteObject( o._package );
            s.WriteObject( o._resources );
            s.Writer.WriteNonNegativeSmallInt32( o._index );
        }

        public bool IsAfter => true;

        public int Index => _index;

        public IEnumerable<IResPackageResources> Reachables => _package.AfterReachablePackages.Select( p => p.ResourcesAfter ).Append( _package.Resources );

        public IResourceContainer Resources => _resources;

        public ResPackage Package => _package;

        public string? LocalPath => _localPath;
    }
}
