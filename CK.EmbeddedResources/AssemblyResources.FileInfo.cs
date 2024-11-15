using Microsoft.Extensions.FileProviders;
using System;
using System.IO;

namespace CK.Core;

public sealed partial class AssemblyResources
{
    sealed class FileInfo : IFileInfo
    {
        readonly string _path;
        readonly AssemblyResources _r;
        long _length;
        string? _name;

        public FileInfo( AssemblyResources r, string path )
        {
            _path = path;
            _r = r;
            _length = -1;
        }

        public bool Exists => true;

        public string? PhysicalPath => null;

        // This will alwost never called. Avoid the string manipulation when unused.
        public string Name => _name ??= Path.GetFileName( _path );

        public bool IsDirectory => false;

        // Should be rarely called... But who knowns.
        public long Length
        {
            get
            {
                if( _length == -1 )
                {
                    using var s = CreateReadStream();
                    _length = s.Length;
                }
                return _length;
            }
        }

        public DateTimeOffset LastModified => _r.GetLastModified();

        public Stream CreateReadStream() => _r._a.OpenResourceStream( _path );
    }

    sealed class NullFileInfo : IFileInfo
    {
        readonly string _path;
        string? _name;

        public NullFileInfo( string path ) => _path = path;

        public bool Exists => false;

        public long Length => -1;

        public string? PhysicalPath => null;

        // This will alwost never called. Avoid the string manipulation when unused.
        public string Name => _name ??= Path.GetFileName( _path );

        public DateTimeOffset LastModified => Util.UtcMinValue;

        public bool IsDirectory => false;

        public Stream CreateReadStream() => throw new InvalidOperationException();
    }

}
