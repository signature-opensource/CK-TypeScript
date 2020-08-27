using CK.Setup;
using System;
using System.Reflection;

namespace CK.StObj.TypeScript
{
    /// <summary>
    /// Triggers TypeScript code generation fo the decorated class, struct, interface or enum.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Enum )]
    public class TypeScriptAttribute : ContextBoundDelegationAttribute
    {
        string? _folder;
        string? _fileName;
        Type? _sameFoldeAs;

        public TypeScriptAttribute()
            : base( "CK.StObj.TypeScript.Engine.TypeScriptImpl, CK.StObj.TypeScript.Engine" )
        {
        }

        /// <summary>
        /// Gets or sets an optional sub folder that will contain the TypeScript generated code.
        /// There must be no leading '/' or '\': the path is relative to the TypeScript output path of each <see cref="BinPathConfiguration"/>.
        /// <para>
        /// This folder cannot be set to a non null path if <see cref="SameFolderAs"/> is set to a non null type.
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
                        throw new ArgumentException( "Folder must not be rooted: " + value );
                    }
                    if( _sameFoldeAs != null ) throw new InvalidOperationException( "SameFolderAs cannot be set when Folder is not null." );
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
                        throw new ArgumentException( "FileName must end with '.ts': " + value );
                    }
                }
                _fileName = value;
            }
        }

        /// <summary>
        /// Gets or sets the type name to use for this type.
        /// This takes precedence over the <see cref="CK.Core.ExternalNameAttribute"/> that itself
        /// takes precedence over the <see cref="MemberInfo.Name"/> of the type.
        /// </summary>
        public string? TypeName { get; set; }

        /// <summary>
        /// Gets or sets a type which defines this <see cref="Folder"/>.
        /// This Folder MUST be null otherwise an <see cref="InvalidOperationException"/> is raised (and Folder can be set to
        /// a non null path only if this SameFolderAs is null).
        /// </summary>
        public Type? SameFolderAs
        {
            get => _sameFoldeAs;
            set
            {
                if( value != null && _folder != null ) throw new InvalidOperationException( "SameFolderAs cannot be set when Folder is not null." );
                _sameFoldeAs = value;
            }
        }
    }
}
