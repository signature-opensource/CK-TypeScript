using CK.Core;
using System;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Captures a TypeScript type name, target file and folder, the <see cref="ITSType.DefaultValueSource"/>,
    /// an implementation of <see cref="ITSType.TryWriteValue(ITSCodeWriter, object)"/> and an
    /// <see cref="Implementor"/> function that can generate the TypeScript code.
    /// <para>
    /// This is totally mutable and not initialized at first except the C# <see cref="Type"/> that
    /// cannot be changed.
    /// </para>
    /// </summary>
    public sealed class TSGeneratedTypeBuilder
    {
        Type _type;
        string? _folder;
        string? _fileName;
        Type? _sameFolderAs;
        Type? _sameFileAs;
        string? _typeName;

        /// <summary>
        /// Initializes a new descriptor for a C# type.
        /// </summary>
        /// <param name="type">The key C# type.</param>
        public TSGeneratedTypeBuilder( Type type )
        {
            Throw.CheckNotNullArgument( type );
            _type = type;
        }

        /// <summary>
        /// Gets the C# type.
        /// </summary>
        public Type Type => _type;

        /// <summary>
        /// Gets or sets the type script code source that initializes
        /// a default value of this type.
        /// </summary>
        public string? DefaultValueSource { get; set; }

        /// <summary>
        /// Gets or sets a function that can implement <see cref="ITSType.TryWriteValue(ITSCodeWriter, object)"/>
        /// if the generated type corresponds to a C# type that can be expressed as a TypeScript construct.
        /// </summary>
        public Func<ITSCodeWriter, object, bool>? TryWriteValueImplementation { get; set; }

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
        /// <para>
        /// When let or set to null, the C# type name is used.
        /// When set to a non null string, it must not be empty or white space.
        /// </para>
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

        /// <summary>
        /// Gets or sets a function that will be called to generate the code.
        /// To compose a function with this one, use the <see cref="AddImplementor(TSCodeGenerator, bool)"/> helper.
        /// </summary>
        public TSCodeGenerator? Implementor { get; set; }

        /// <summary>
        /// Combines an implementor function with the current <see cref="Implementor"/>.
        /// </summary>
        /// <param name="newOne">The implementor to call before or after the current one.</param>
        /// <param name="prepend">
        /// True to first call <paramref name="newOne"/> before the existing <see cref="Implementor"/> (if any).
        /// By default the existing <see cref="Implementor"/> is called before <paramref name="newOne"/>).
        /// </param>
        public void AddImplementor( TSCodeGenerator newOne, bool prepend = false )
        {
            Throw.CheckNotNullArgument( newOne );
            if( Implementor != null )
            {
                var captured = Implementor;
                Implementor = ( m, f ) => prepend
                                            ? newOne( m, f ) && captured( m, f )
                                            : captured( m, f ) && newOne( m, f );
            }
            else
            {
                Implementor = newOne;
            }
        }


    }

}

