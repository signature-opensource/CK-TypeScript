using Microsoft.Extensions.FileProviders;
using System.Collections;
using System.Collections.Generic;

namespace CK.Core;

public sealed partial class AssemblyResources
{
    sealed class DirectoryContents : IDirectoryContents
    {
        readonly AssemblyResources _r;
        readonly string _prefix;

        public DirectoryContents( AssemblyResources r, string prefix )
        {
            _r = r;
            _prefix = prefix;
        }

        public bool Exists => true;

        public IEnumerator<IFileInfo> GetEnumerator()
        {
            var (idx, len) = ImmutableOrdinalSortedStrings.GetPrefixedRange( _prefix, _r.CKResourceNames.Span );
            var range = _r.CKResourceNames.Slice( idx, len );
            for( int i = 0; i < range.Length; ++i )
            {
                yield return new FileInfo( _r, range.Span[i] );
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

}
