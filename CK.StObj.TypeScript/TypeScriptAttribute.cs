using CK.Core;
using CK.Setup;
using System;
using System.Reflection;

namespace CK.StObj.TypeScript
{
    /// <summary>
    /// Configures TypeScript code generation for the decorated enumeration, class, struct or interface.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Enum )]
    public class TypeScriptAttribute : ContextBoundDelegationAttribute
    {
        string? _folder;
        string? _fileName;
        Type? _sameFolderAs;
        Type? _sameFileAs;
        string? _typeName;

        /// <summary>
        /// Initializes a new empty <see cref="TypeScriptAttribute"/>.
        /// </summary>
        public TypeScriptAttribute()
            : base( "CK.StObj.TypeScript.Engine.TypeScriptAttributeImpl, CK.StObj.TypeScript.Engine" )
        {
        }

        /// <summary>
        /// Base class constructor for specialized <see cref="TypeScriptAttribute"/> that
        /// can be bound to a specialized implementation that supports type generator
        /// (the ITSCodeGeneratorType from CK.StObj.TypeScript.Engine).
        /// <para>
        /// 
        /// </para>
        /// </summary>
        protected TypeScriptAttribute( string actualAttributeTypeAssemblyQualifiedName )
            : base( actualAttributeTypeAssemblyQualifiedName )
        {
        }

        /// <summary>
        /// Gets or sets an optional sub folder that will contain the TypeScript generated code.
        /// There must be no leading '/' or '\': the path is relative to the TypeScript output path of each <see cref="BinPathConfiguration"/>.
        /// <para>
        /// This folder cannot be set to a non null path if <see cref="SameFolderAs"/> or <see cref="SameFileAs"/> is set to a non null type.
        /// </para>
        /// <para>
        /// When let to null, the folder will be derived from the type's namespace (unless <see cref="SameFolderAs"/> is set).
        /// When <see cref="string.Empty"/>, the file will be in the root folder of the TypeScript output path.
        /// </para>
        /// </summary>
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

        /// <summary>
        /// Gets or sets the file name that will contain the TypeScript generated code.
        /// When not null, this must be a valid file name that ends with a '.ts' extension.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the type name to use for this type.
        /// This takes precedence over the <see cref="ExternalNameAttribute"/> that itself
        /// takes precedence over the <see cref="MemberInfo.Name"/> of the type.
        /// </summary>
        public string? TypeName
        {
            get => _typeName;
            set
            {
                Throw.CheckArgument( value == null || !string.IsNullOrWhiteSpace( value ) );
                _typeName = value;
            }
        }

        /// <summary>
        /// Gets or sets another type which defines the <see cref="Folder"/>.
        /// Folder MUST be null and <see cref="SameFileAs"/> must be null or be the same as the new value otherwise
        /// an <see cref="InvalidOperationException"/> is raised.
        /// <para>
        /// This defaults to <see cref="SameFileAs"/>.
        /// </para>
        /// </summary>
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

        /// <summary>
        /// Gets or sets another type which defines the final <see cref="Folder"/> and <see cref="FileName"/>.
        /// Both Folder and FileName MUST be null otherwise an <see cref="InvalidOperationException"/> is raised (conversely, Folder and FileName can
        /// be set to non null values only if this SameFileAs is null).
        /// </summary>
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
}
