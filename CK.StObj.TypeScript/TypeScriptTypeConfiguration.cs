using CK.Core;
using CK.StObj.TypeScript;
using System;
using System.Xml.Linq;

namespace CK.Setup
{
    /// <summary>
    /// Configuration of a type that must be generated. This is the external version of <see cref="TypeScriptAttribute"/>.
    /// <para>
    /// <see cref="ToAttribute(IActivityMonitor, Func{IActivityMonitor, string, Type?})"/> checks the validity and creates the
    /// corresponding attribute. Then <see cref="TypeScriptAttribute.ApplyOverride(TypeScriptAttribute?)"/> is used if the
    /// type is decorated with the TypeScriptAttribute. The configuration overrides the declare attribute values.
    /// </para>
    /// </summary>
    public sealed class TypeScriptTypeConfiguration
    {
        /// <summary>
        /// Initializes a new <see cref="TypeScriptTypeConfiguration"/>.
        /// </summary>
        /// <param name="type">See <see cref="Type"/>.</param>
        /// <param name="typeName">See <see cref="TypeName"/>.</param>
        /// <param name="folder">See <see cref="Folder"/>.</param>
        /// <param name="fileName">See <see cref="FileName"/>.</param>
        /// <param name="sameFolderAs">See <see cref="SameFolderAs"/>.</param>
        /// <param name="sameFileAs">See <see cref="SameFileAs"/>.</param>
        public TypeScriptTypeConfiguration( string type,
                                            string? typeName = null,
                                            string? folder = null,
                                            string? fileName = null,
                                            string? sameFolderAs = null,
                                            string? sameFileAs = null )
        {
            Throw.CheckNotNullOrWhiteSpaceArgument( type );
            Throw.CheckArgument( typeName == null || !string.IsNullOrWhiteSpace( typeName ) );
            Throw.CheckArgument( folder == null || !string.IsNullOrWhiteSpace( folder ) );
            Throw.CheckArgument( sameFileAs == null || !string.IsNullOrWhiteSpace( sameFileAs ) );
            Throw.CheckArgument( sameFolderAs == null || !string.IsNullOrWhiteSpace( sameFolderAs ) );
            Type = type;
            TypeName = typeName;
            Folder = folder;
            FileName = fileName;
            SameFolderAs = sameFolderAs;
            SameFileAs = sameFileAs;
        }

        /// <summary>
        /// Initializes a new configuration value from its xml representation.
        /// </summary>
        /// <param name="e">The xml element.</param>
        public TypeScriptTypeConfiguration( XElement e )
        {
            Type = (string?)e.Attribute( StObjEngineConfiguration.xType ) ?? e.Value;
            TypeName = (string?)e.Attribute( TypeScriptAspectConfiguration.xTypeName );
            Folder = (string?)e.Attribute( TypeScriptAspectConfiguration.xFolder );
            FileName = (string?)e.Attribute( TypeScriptAspectConfiguration.xFileName );
            SameFolderAs = (string?)e.Attribute( TypeScriptAspectConfiguration.xSameFolderAs );
            SameFileAs = (string?)e.Attribute( TypeScriptAspectConfiguration.xSameFileAs );
        }

        /// <summary>
        /// Returns the xml representation.
        /// </summary>
        /// <returns>The <see cref="XElement"/>.</returns>
        public XElement ToXml()
        {
            return new XElement( StObjEngineConfiguration.xType,
                                 TypeName != null ? new XAttribute( TypeScriptAspectConfiguration.xTypeName, TypeName ) : null,
                                 Folder != null ? new XAttribute( TypeScriptAspectConfiguration.xFolder, Folder ) : null,
                                 FileName != null ? new XAttribute( TypeScriptAspectConfiguration.xFileName, FileName ) : null,
                                 SameFolderAs != null ? new XAttribute( TypeScriptAspectConfiguration.xSameFolderAs, SameFolderAs ) : null,
                                 SameFileAs != null ? new XAttribute( TypeScriptAspectConfiguration.xSameFileAs, SameFileAs ) : null,
                                 Type );
        }

        /// <summary>
        /// Gets or sets the type assembly qualified name name that must be generated. For IPoco, it is the primary
        /// interface assemby qualified name or <see cref="ExternalNameAttribute"/>.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the TypeScript type name to use. See <see cref="TypeScriptAttribute.TypeName"/>.
        /// </summary>
        public string? TypeName { get; set; }

        /// <summary>
        /// See <see cref="TypeScriptAttribute.Folder"/>.
        /// </summary>
        public string? Folder { get; set; }

        /// <summary>
        /// See <see cref="TypeScriptAttribute.FileName"/>.
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// See <see cref="TypeScriptAttribute.SameFolderAs"/>.
        /// <para>
        /// This is the assembly qualified name name of the type (for IPoco, it is the primary
        /// interface assemby qualified name or <see cref="ExternalNameAttribute"/>).
        /// </para>
        /// </summary>
        public string? SameFolderAs { get; set; }

        /// <summary>
        /// See <see cref="TypeScriptAttribute.SameFileAs"/>.
        /// <para>
        /// This is the assembly qualified name name of the type (for IPoco, it is the primary
        /// interface assemby qualified name or <see cref="ExternalNameAttribute"/>).
        /// </para>
        /// </summary>
        public string? SameFileAs { get; set; }

        /// <summary>
        /// Tries to create a <see cref="TypeScriptAttribute"/> from this configuration.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="typeResolver">Type resolver for <see cref="SameFolderAs"/> and <see cref="SameFileAs"/>.</param>
        /// <returns>The type script arrtibute or null on error.</returns>
        public TypeScriptAttribute? ToAttribute( IActivityMonitor monitor, Func<IActivityMonitor,string,Type?> typeResolver )
        {
            bool success = true;
            if( TypeName != null ) success &= ValidNotEmpty( monitor, this, "TypeName", TypeName );
            Type? tSameFile = null;
            if( SameFileAs != null )
            {
                tSameFile = ResolveType( monitor, this, typeResolver, "SameFileAs", SameFileAs );
                success &= tSameFile != null; 
                if( FileName != null ) success &= Error( monitor, this, "FileName", "SameFileAs" );
                if( Folder != null ) success &= Error( monitor, this, "Folder", "SameFileAs" );
                if( SameFolderAs != null ) success &= Error( monitor, this, "SameFolderAs", "SameFileAs" );
            }
            Type? tSameFolder = null;
            if( SameFolderAs != null )
            {
                tSameFolder = ResolveType( monitor, this, typeResolver, "SameFolderAs", SameFolderAs );
                success &= tSameFile != null;
                if( Folder != null ) success &= Error( monitor, this, "Folder", "SameFolderAs" );
            }
            return success
                    ? new TypeScriptAttribute() { FileName = FileName, Folder = Folder, SameFileAs = tSameFile, SameFolderAs = tSameFolder }
                    : null;

            static bool Error( IActivityMonitor monitor, TypeScriptTypeConfiguration c, string n1, string n2 )
            {
                monitor.Error( $"TypeScriptAspect configuration error: '{n1}' and '{n2}' cannot be both set at the same time in: {c.ToXml()}" );
                return false;
            }

            static Type? ResolveType( IActivityMonitor monitor, TypeScriptTypeConfiguration c, Func<IActivityMonitor, string, Type?> typeResolver, string name, string value )
            {
                if( !ValidNotEmpty( monitor, c, name, value ) )
                {
                    return null;
                }
                var t = typeResolver( monitor, value );
                if( t == null )
                {
                    monitor.Error( $"TypeScriptAspect configuration error: unable to resolve type for '{name}': '{value}' in:{Environment.NewLine}{c.ToXml()}" );
                    return null;
                }
                return t;
            }

            static bool ValidNotEmpty( IActivityMonitor monitor, TypeScriptTypeConfiguration c, string name, string value )
            {
                if( string.IsNullOrWhiteSpace( value ) )
                {
                    monitor.Error( $"TypeScriptAspect configuration error: '{name}' cannot be empty or whitespace in:{Environment.NewLine}{c.ToXml()}" );
                    return false;
                }
                return true;
            }
        }
    }

}
