using Microsoft.Extensions.FileProviders;
using System;
using System.IO;

namespace CK.Core;

public sealed partial class AssemblyResources
{
    internal sealed class FileInfo : IFileInfo
    {
        readonly string _path;
        readonly FileProvider _fileProvider;
        readonly string _name;
        long _fileLength;

        internal FileInfo( FileProvider r, string path, string name )
        {
            _path = path;
            _name = name;
            _fileProvider = r;
            _fileLength = -1;
        }

        internal FileProvider FileProvider => _fileProvider;

        internal string Path => _path;

        public bool Exists => true;

        public string? PhysicalPath => null;

        public string Name => _name;

        public bool IsDirectory => false;

        // Should be rarely called... But who knowns.
        public long Length
        {
            get
            {
                if( _fileLength == -1 )
                {
                    using var s = CreateReadStream();
                    _fileLength = s.Length;
                }
                return _fileLength;
            }
        }

        public DateTimeOffset LastModified => _fileProvider.AssemblyResources.GetLastModified();

        public Stream CreateReadStream() => _fileProvider.AssemblyResources.OpenResourceStream( _path );

        public override string ToString() => _path;
    }

}
