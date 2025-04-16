using CK.BinarySerialization;
using CK.EmbeddedResources;
using System.ComponentModel;

namespace CK.Core;

public sealed partial class ResPackage
{
    [SerializationVersion(0)]
    sealed class BeforeRes : IResPackageResources, ICKSlicedSerializable
    {
        readonly ResPackage _package;
        readonly IResourceContainer _resources;
        readonly string? _localPath;
        readonly int _index;

        public BeforeRes( ResPackage package, IResourceContainer resources, int index )
        {
            _package = package;
            _resources = resources;
            _index = index;
            _localPath = GetLocalPath( resources );
        }

        internal static string? GetLocalPath( IResourceContainer resources ) => resources is StoreContainer c
                                                                                ? c.LocalPath
                                                                                : resources is FileSystemResourceContainer f
                                                                                  && f.HasLocalFilePathSupport
                                                                                    ? f.ResourcePrefix
                                                                                    : null;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public BeforeRes( IBinaryDeserializer d, ITypeReadInfo info )
        {
            _package = d.ReadObject<ResPackage>();
            _resources = d.ReadObject<IResourceContainer>();
            _index = d.Reader.ReadNonNegativeSmallInt32();
            _localPath = GetLocalPath( _resources );
        }

        [EditorBrowsable( EditorBrowsableState.Never )]
        public static void Write( IBinarySerializer s, in BeforeRes o )
        {
            s.WriteObject( o._package );
            s.WriteObject( o._resources );
            s.Writer.WriteNonNegativeSmallInt32( o._index );
        }

        public bool IsAfter => false;

        public int Index => _index;

        public bool IsCodeResources => false;

        public bool IsAppResources => _package.FullName == "<App>";

        public IResourceContainer Resources => _resources;

        public ResPackage Package => _package;

        public string? LocalPath => _localPath;

        public override string ToString() => $"Before {_package}";
    }
}
