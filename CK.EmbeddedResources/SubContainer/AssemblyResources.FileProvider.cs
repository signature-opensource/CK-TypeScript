using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;
using System.IO;

namespace CK.Core;

public sealed partial class AssemblyResources
{
    internal sealed class FileProvider : IFileProvider
    {
        readonly AssemblyResources _assemblyResources;
        readonly string _prefix;
        readonly ReadOnlyMemory<string> _resourceNames;
        DirectoryContents? _rootContent;

        public FileProvider( AssemblyResources assemblyResources, string prefix, ReadOnlyMemory<string> resourceNames )
        {
            _assemblyResources = assemblyResources;
            _prefix = prefix;
            _resourceNames = resourceNames;
        }

        public AssemblyResources AssemblyResources => _assemblyResources;

        public ReadOnlyMemory<string> ResourceNames => _resourceNames;

        public string Prefix => _prefix;

        public IDirectoryContents GetDirectoryContents( string subpath )
        {
            if( string.IsNullOrEmpty( subpath ) )
            {
                return _rootContent ??= new DirectoryContents( this );
            }
            if( subpath[^1] != '/' )
            {
                subpath = _prefix + subpath + '/';
            }
            else
            {
                subpath = _prefix + subpath;
            }
            var (idx, len) = ImmutableOrdinalSortedStrings.GetPrefixedRange( subpath, _resourceNames.Span );
            return len > 0
                    ? new DirectoryContents( this, subpath, idx, len, null )
                    : NotFoundDirectoryContents.Singleton;
        }

        public IFileInfo GetFileInfo( string subpath )
        {
            Throw.CheckNotNullOrEmptyArgument( subpath );
            Throw.CheckArgument( !subpath.Contains( '\\' ) );
            subpath = _prefix + subpath;
            if( subpath[^1] != '/' )
            {
                int idxFile = ImmutableOrdinalSortedStrings.IndexOf( subpath, _resourceNames.Span );
                if( idxFile >= 0 )
                {
                    return new FileInfo( this, subpath, Path.GetFileName( subpath ) );
                }
                subpath += '/';
            }
            var (idx, len) = ImmutableOrdinalSortedStrings.GetPrefixedRange( subpath, _resourceNames.Span );
            return len > 0
                    ? new DirectoryContents( this, subpath, idx, len, null )
                    : new NotFoundFileInfo( subpath );
        }

        public IChangeToken Watch( string filter ) => NullChangeToken.Singleton;
    }

}
