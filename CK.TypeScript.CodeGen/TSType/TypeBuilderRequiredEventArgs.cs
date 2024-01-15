using CK.Core;
using System;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Event arguments that acts as a builder of <see cref="ITSGeneratedType"/>.
    /// This is raised when a C# type must be resolved.
    /// <para>
    /// Captures a TypeScript type name, target file and folder, the <see cref="ITSType.DefaultValueSource"/>,
    /// an implementation of <see cref="ITSType.TryWriteValue(ITSCodeWriter, object)"/> and an
    /// <see cref="Implementor"/> function that can generate the TypeScript code.
    /// </para>
    /// <para>
    /// This is totally mutable and not initialized at first except the <see cref="Type"/> that
    /// cannot be changed.
    /// </para>
    /// </summary>
    public sealed class TypeBuilderRequiredEventArgs : EventMonitoredArgs
    {
        readonly Type _type;
        readonly string _defaultTypeName;
        string? _folder;
        string? _fileName;
        Type? _sameFolderAs;
        Type? _sameFileAs;
        string? _typeName;
        string? _defaultValueSource;
        DefaultValueSourceProvider? _defaultValueSourceProvider;
        TSValueWriter? _tryWriteValueImplementation;
        TSCodeGenerator? _implementor;
        ITSType? _resolved;
        bool _hasError;

        public TypeBuilderRequiredEventArgs( IActivityMonitor monitor, Type type, string defaultTypeName )
            : base( monitor )
        {
            Throw.CheckNotNullArgument( type );
            _type = type;
            _defaultTypeName = defaultTypeName;
        }

        /// <summary>
        /// Gets the key type.
        /// </summary>
        public Type Type => _type;

        /// <summary>
        /// Gets or sets the <see cref="ITSType"/> to use for this C# type. This can be a <see cref="ITSGeneratedType"/>
        /// (bound to a file) or a <see cref="TSType"/>.
        /// <para>
        /// When this type is set, all other properties are ignored.
        /// </para>
        /// </summary>
        public ITSType? ResolvedType
        {
            get => _resolved;
            set => _resolved = value;
        }

        /// <summary>
        /// Gets the default type name.
        /// This may not be a valid name (generics for example).
        /// </summary>
        public string DefaultTypeName => _defaultTypeName;

        /// <summary>
        /// Tries to initializes all the configurable properties at once whithout throwing: errors are logged
        /// but <see cref="SetError"/> is not called, it's up to the caller to decide whether an initialization
        /// error impedes the generated type to be successfully handled.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="folder">See <see cref="Folder"/>.</param>
        /// <param name="fileName">See <see cref="FileName"/>.</param>
        /// <param name="typeName">See <see cref="TypeName"/>.</param>
        /// <param name="sameFolderAs">See <see cref="SameFolderAs"/>.</param>
        /// <param name="sameFileAs">See <see cref="SameFileAs"/>.</param>
        /// <returns>True on success, false on error.</returns>
        public bool TryInitialize( IActivityMonitor monitor,
                                   string? folder,
                                   string? fileName,
                                   string? typeName,
                                   Type? sameFolderAs,
                                   Type? sameFileAs )
        {
            bool success = true;
            _folder = folder;
            if( folder != null )
            {
                folder = folder.Trim();
                if( folder.Length > 0 )
                {
                    if( folder[0] == '/' || folder[0] == '\\' )
                    {
                        monitor.Error( $"Folder must not be rooted: {folder}" );
                        success = false;
                    }
                    var e = TypeScriptFolder.GetPathError( folder, true );
                    if( e != null )
                    {
                        monitor.Error( $"Invalid Folder. {e}" );
                        success = false;
                    }
                }
            }
            _fileName = fileName;
            if( fileName != null )
            {
                var e = TypeScriptFolder.GetPathError( fileName, false );
                if( e != null )
                {
                    monitor.Error( $"Invalid FileName. {e}" );
                    success = false;
                }
            }
            _typeName = typeName;
            if( typeName != null && string.IsNullOrWhiteSpace( typeName ) )
            {
                monitor.Error( $"If TypeName is not null it must be not empty or whitespace." );
                success = false;
            }
            _sameFolderAs = sameFolderAs;
            if( sameFolderAs != null )
            {
                if( _folder != null )
                {
                    monitor.Error( "SameFolderAs cannot be set when Folder is used." );
                    success = false;
                }
            }
            _sameFileAs = sameFileAs;
            if( sameFileAs != null )
            {
                if( _folder != null )
                {
                    monitor.Error( "SameFileAs cannot be set when Folder is used." );
                    success = false;
                }
                if( _fileName != null )
                {
                    monitor.Error( "SameFileAs cannot be set when FileName is used." );
                    success = false;
                }

                if( _sameFolderAs != null && _sameFolderAs != sameFileAs )
                {
                    monitor.Error( "SameFileAs cannot be set when SameFolderAs is not null (except to the same type)." );
                    success = false;
                }
            }
            return success;
        }

        /// <summary>
        /// Gets or sets the type script code source that initializes
        /// a default value of this type.
        /// <para>
        /// When let to null, there is no default value for the type.
        /// When set to a not null string, it must not be empty or whitespace.
        /// </para>
        /// <para>
        /// Setting this clears any previously set <see cref="DefaultValueSourceProvider"/>.
        /// </para>
        /// </summary>
        public string? DefaultValueSource
        {
            get => _defaultValueSource;
            set
            {
                if( _defaultValueSource != value )
                {
                    Throw.CheckArgument( value == null || !string.IsNullOrWhiteSpace( value ) );
                    _defaultValueSource = value;
                }
                _defaultValueSourceProvider = null;
            }
        }

        /// <summary>
        /// Gets or sets a function that will compute <see cref="DefaultValueSource"/> when needed.
        /// This is required for composites when their default value are built upon their components
        /// default values. 
        /// <para>
        /// Setting this (even to null) clears any previously set <see cref="DefaultValueSource"/>.
        /// </para>
        /// </summary>
        public DefaultValueSourceProvider? DefaultValueSourceProvider
        {
            get => _defaultValueSourceProvider;
            set
            {
                _defaultValueSourceProvider = value;
                _defaultValueSource = null;
            }
        }

        /// <summary>
        /// Gets whether <see cref="SetError"/> has been called.
        /// </summary>
        public bool HasError => _hasError;

        /// <summary>
        /// States that an error occurred. Further handling of this type should be skipped.
        /// </summary>
        public void SetError() => _hasError = true;

        /// <summary>
        /// Gets or sets an optional sub folder that will contain the TypeScript generated code.
        /// There must be no leading '/' or '\': the path is relative to the root <see cref="TypeScriptRoot"/>.
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

        /// Gets or sets a function that can implement <see cref="ITSType.TryWriteValue(ITSCodeWriter, object)"/>
        /// if a value of the generated type corresponds can be expressed as a TypeScript construct.
        /// </summary>
        public TSValueWriter? TryWriteValueImplementation
        {
            get => _tryWriteValueImplementation;
            set => _tryWriteValueImplementation = value;
        }

        /// <summary>
        /// Gets or sets a function that will be called to generate the code.
        /// To compose a function with this one, use the <see cref="AddImplementor(TSCodeGenerator, bool)"/> helper.
        /// </summary>
        public TSCodeGenerator? Implementor
        {
            get => _implementor;
            set => _implementor = value;
        }

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
            if( _implementor != null )
            {
                var captured = _implementor;
                _implementor = ( m, f ) => prepend
                                            ? newOne( m, f ) && captured( m, f )
                                            : captured( m, f ) && newOne( m, f );
            }
            else
            {
                _implementor = newOne;
            }
        }

    }

}

