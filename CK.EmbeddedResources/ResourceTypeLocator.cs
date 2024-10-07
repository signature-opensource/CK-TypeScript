using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace CK.Core;

/// <summary>
/// Locator for a resource from a type and a resource name in the type's assembly.
/// <para>
/// This should always be initialized from the <see cref="Assembly.GetManifestResourceNames()"/>
/// of the declarer's assembly.
/// </para>
/// <para>
/// This is a record struct that benefits of the ToString (PrinMembers) and equality code generation but with
/// an explicit constructor to handle the only <c>default</c> value to be <see cref="IsValid"/> false.
/// The <c>default</c> value makes <see cref="Nullable{T}"/> useless for this type.
/// </para>
/// </summary>
public readonly record struct ResourceTypeLocator
{
    /// <summary>
    /// Initializes a new resource locator.
    /// </summary>
    /// <param name="declarer">The type that declares this resource.</param>
    /// <param name="resourceName">the resource name in the <paramref name="declarer"/>'s assembly. Must not be null, empty or whitespace.</param>
    public ResourceTypeLocator( Type declarer, string resourceName )
    {
        Throw.CheckNotNullArgument( declarer );
        Throw.CheckNotNullOrWhiteSpaceArgument( resourceName );
        Declarer = declarer;
        ResourceName = resourceName;
    }

    /// <summary>
    /// Gets whether this locator is valid: the <see cref="Declarer"/> is not null
    /// and the <see cref="ResourceName"/> is not null, empty or whitespace.
    /// <para>
    /// Whether the resource exists or not is not known.
    /// </para>
    /// Only the <c>default</c> of this type is invalid.
    /// </summary>
    public bool IsValid => Declarer != null;

    /// <summary>
    /// Gets the type that declares this resource.
    /// </summary>
    public Type Declarer { get; }

    /// <summary>
    /// Gets the resource name in the <see cref="Declarer"/>'s assembly.
    /// </summary>
    public string ResourceName { get; }

    /// <summary>
    /// Gets the resource content.
    /// If a stream cannot be obtained, a detailed <see cref="IOException"/> is raised.
    /// </summary>
    /// <returns>The resource's content stream.</returns>
    public Stream GetStream()
    {
        var s = Declarer.Assembly.GetManifestResourceStream( ResourceName );
        return s ?? ThrowDetailedError();
    }

    readonly bool PrintMembers( StringBuilder b )
    {
        b.Append( nameof( ResourceName ) ).Append( " = \"" ).Append( ResourceName ).Append( "\", " )
         .Append( nameof( Declarer ) ).Append( " = " ).Append( Declarer );
        return true;
    }

    [StackTraceHidden]
    Stream ThrowDetailedError()
    {
        var b = new StringBuilder();
        b.Append( $"Resource '{ToString()}' cannot be loaded." );
        var info = Declarer.Assembly.GetManifestResourceInfo( ResourceName );
        if( info == null )
        {
            b.AppendLine( " No information for this resource. The ResourceName may not exist at all. Resource names are:" );
            foreach( var n in Declarer.Assembly.GetManifestResourceNames() )
            {
                b.AppendLine( n );
            }
        }
        else
        {
            b.AppendLine( "ManifestResourceInfo:" )
             .Append( "ReferencedAssembly = " ).Append( info.ReferencedAssembly ).AppendLine()
             .Append( "FileName = " ).Append( info.FileName ).AppendLine()
             .Append( "ResourceLocation = " ).Append( info.ResourceLocation ).AppendLine();
        }
        throw new IOException( b.ToString() );
    }
}
