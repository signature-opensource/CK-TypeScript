using Microsoft.Extensions.FileProviders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CK.Core;

public sealed partial class AssemblyResources
{
    sealed class DirectoryContents : IDirectoryContents, IFileInfo
    {
        readonly FileProvider _provider;
        readonly string _path;
        int _idx;
        int _count;
        string _name;

        public DirectoryContents( FileProvider r )
        {
            _provider = r;
            _path = r.Prefix;
            _name = "Res";
            _count = r.ResourceNames.Length;
        }

        public DirectoryContents( FileProvider r, string path, int idx, int count, string? name )
        {
            Throw.DebugAssert( path[^1] == '/' );
            _provider = r;
            _path = path;
            _idx = idx;
            _count = count;
            _name = name ?? new string( Path.GetFileName( path.AsSpan( 0, path.Length - 1 ) ) );
        }

        public bool Exists => true;

        public long Length => -1;

        public string? PhysicalPath => null;

        public string Name => _name;

        public DateTimeOffset LastModified => _provider.AssemblyResources.GetLastModified();

        public bool IsDirectory => true;

        public Stream CreateReadStream()
        {
            throw new InvalidOperationException( "IDirectoryContent has no content stream." );
        }

        public IEnumerator<IFileInfo> GetEnumerator()
        {
            for( int i = 0; i < _count; ++i )
            {
                var currentIdx = _idx + i;
                var p = _provider.ResourceNames.Span[currentIdx];
                int idxSub = p.IndexOf( '/', _path.Length );
                if( idxSub < 0 )
                {
                    yield return new FileInfo( _provider, p, p.Substring( _path.Length ) );
                }
                else
                {
                    p = p.Remove( idxSub + 1 );
                    var len = ImmutableOrdinalSortedStrings.GetEndIndex( p, _provider.ResourceNames.Span.Slice( currentIdx ) );
                    Throw.DebugAssert( len > 0 );
                    Throw.DebugAssert( (currentIdx, len) == ImmutableOrdinalSortedStrings.GetPrefixedRange( p, _provider.ResourceNames.Span ) );
                    yield return new DirectoryContents( _provider, p, currentIdx, len, p.Substring( _path.Length, p.Length - _path.Length - 1 ) );
                    i += (len - 1);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString() => _path;
    }

}
