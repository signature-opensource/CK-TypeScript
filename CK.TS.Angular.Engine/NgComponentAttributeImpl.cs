using CK.Core;
using CK.Setup;
using CK.StObj.TypeScript;
using CK.StObj.TypeScript.Engine;
using CK.TypeScript.CodeGen;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace CK.TS.Angular.Engine;

/// <summary>
/// Implements <see cref="NgModuleAttribute"/>.
/// </summary>
public partial class NgComponentAttributeImpl : TypeScriptPackageAttributeImpl
{
    readonly string _snakeName;

    /// <summary>
    /// Initializes a new <see cref="NgComponentAttributeImpl"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="attr">The attribute.</param>
    /// <param name="type">The decorated type.</param>
    public NgComponentAttributeImpl( IActivityMonitor monitor, NgComponentAttribute attr, Type type )
        : base( monitor, attr, type )
    {
        if( !typeof( NgComponent ).IsAssignableFrom( type ) )
        {
            monitor.Error( $"[NgComponent] can only decorate a NgComponent: '{type:N}' is not a NgComponent." );
        }
        _snakeName = CheckComponentName( monitor, type, "Component" );
        SetTypeScriptFolder( TypeScriptFolder.AppendPart( _snakeName ) );
    }

    internal static string CheckComponentName( IActivityMonitor monitor, Type type, string kind )
    {
        if( !type.IsSealed )
        {
            monitor.Error( $"Ng{kind} must be sealed (specialization is not supported): '{type:N}' must be sealed." );
        }
        var n = type.Name;
        if( n.Length <= kind.Length || !n.EndsWith( kind ) )
        {
            monitor.Error( $"'{type:N}' is a Ng{kind}, its type name must end with \"{kind}\"." );
        }
        else
        {
            n = n.Substring( 0, n.Length - kind.Length );
        }
        return ToSnakeCase().Replace( n, "$1-$2" ).ToLowerInvariant();
    }

    /// <summary>
    /// Gets the component name that is the C# <see cref="TypeScriptFileAttributeImpl.DecoratedType"/> name (with the "Component" suffix).
    /// </summary>
    public string ComponentName => DecoratedType.Name;

    /// <summary>
    /// Gets the component name (snake-case) without "Component" suffix.
    /// </summary>
    public string FileComponentName => _snakeName;

    /// <summary>
    /// Gets the attribute.
    /// </summary>
    public new NgComponentAttribute Attribute => Unsafe.As<NgComponentAttribute>( base.Attribute );

    protected override bool GenerateCode( IActivityMonitor monitor, TypeScriptContext context )
    {
        var fName = _snakeName + ".component.ts";
        if( !Resources.TryGetResource( monitor, fName, out var res ) )
        {
            return false;
        }
        var file = context.Root.Root.CreateResourceFile( in res, TypeScriptFolder.AppendPart( fName ) );
        Throw.DebugAssert( ".ts extension has been checked by Initialize.", file is ResourceTypeScriptFile );
        ITSDeclaredFileType tsType = Unsafe.As<ResourceTypeScriptFile>( file ).DeclareType( ComponentName );

        return base.GenerateCode( monitor, context )
               && context.GetAngularCodeGen().ComponentManager.RegisterComponent( monitor, this, tsType );
    }

    [GeneratedRegex( "([a-z])([A-Z])", RegexOptions.CultureInvariant )]
    private static partial Regex ToSnakeCase();
}
