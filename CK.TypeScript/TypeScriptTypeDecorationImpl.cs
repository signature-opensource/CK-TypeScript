using CK.Core;
using System;

namespace CK.TypeScript;

struct TypeScriptTypeDecorationImpl
{
    string? _typeName;
    string? _folder;
    string? _fileName;
    Type? _sameFolderAs;
    Type? _sameFileAs;

    public TypeScriptTypeDecorationImpl( ITypeScriptTypeDecorationAttribute a )
    {
        _typeName = a.TypeName;
        _folder = a.Folder;
        _fileName = a.FileName;
        _sameFolderAs = a.SameFolderAs;
        _sameFileAs = a.SameFileAs;
    }

    public string? Folder
    {
        get => _folder;
        set
        {
            if( value != null )
            {
                value = value.Trim();
                if( value.Length > 0 && (value[0] == '/' || value[0] == '\\') )
                {
                    Throw.ArgumentException( "value", "Folder must not be rooted: " + value );
                }
                if( _sameFolderAs != null ) Throw.InvalidOperationException( "Folder cannot be set when SameFolderAs is not null." );
                if( _sameFileAs != null ) Throw.InvalidOperationException( "Folder cannot be set when SameFileAs is not null." );
            }
            _folder = value;
        }
    }

    public string? FileName
    {
        get => _fileName;
        set
        {
            if( value != null )
            {
                value = value.Trim();
                if( value.Length <= 3 || !value.EndsWith( ".ts", StringComparison.OrdinalIgnoreCase ) )
                {
                    Throw.ArgumentException( "FileName must end with '.ts': " + value );
                }
                if( _sameFileAs != null ) Throw.InvalidOperationException( "FileName cannot be set when SameFileAs is not null." );
            }
            _fileName = value;
        }
    }

    public string? TypeName
    {
        get => _typeName;
        set
        {
            Throw.CheckNotNullOrWhiteSpaceArgument( value );
            _typeName = value;
        }
    }

    public Type? SameFolderAs
    {
        get => _sameFolderAs ?? _sameFileAs;
        set
        {
            if( value != null )
            {
                if( _folder != null ) Throw.InvalidOperationException( "SameFolderAs cannot be set when Folder is not null." );
                if( _sameFileAs != null && _sameFileAs != value ) Throw.InvalidOperationException( "SameFolderAs cannot be set when SameFileAs is not null (except to the same type)." );
            }
            _sameFolderAs = value;
        }
    }

    public Type? SameFileAs
    {
        get => _sameFileAs;
        set
        {
            if( value != null )
            {
                if( _folder != null ) Throw.InvalidOperationException( "SameFileAs cannot be set when Folder is not null." );
                if( _fileName != null ) Throw.InvalidOperationException( "SameFileAs cannot be set when FileName is not null." );
                if( _sameFolderAs != null && _sameFolderAs != value ) Throw.InvalidOperationException( "SameFileAs cannot be set when SameFolderAs is not null (except to the same type)." );
            }
            _sameFileAs = value;
        }
    }
}
