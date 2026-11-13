using CK.Core;
using System.Linq;
using System.Xml.Linq;

[assembly: CK.Setup.AppBuilderContributor( typeof( CK.Setup.TypeScriptAppBuilderContributor ) )]

namespace CK.Setup;

/// <summary>
/// Configures the TypeScript aspect from the clients declared by the parties of the App ("ckonf.json"): a client whose
/// "Type" is "TypeScript" is a <see cref="TypeScriptBinPathAspectConfiguration"/> of its BinPath (its name is the client
/// name, its <see cref="TypeScriptBinPathAspectConfiguration.ClientConfigurationKeys"/> configure it), and the presence of
/// a client gives the <see cref="TypeScriptAspectConfiguration"/>. These keys are compile time keys: they are not projected
/// in the host's "appsettings.json". The other keys of the client (its options) are the run time's.
/// </summary>
public sealed class TypeScriptAppBuilderContributor : IAppBuilderContributor
{
    readonly AppIdentityAppBuilderContributor _appIdentity;

    /// <summary>
    /// Initializes a new contributor.
    /// </summary>
    /// <param name="appIdentity">The parties and their declared clients.</param>
    public TypeScriptAppBuilderContributor( AppIdentityAppBuilderContributor appIdentity )
    {
        _appIdentity = appIdentity;
    }

    /// <inheritdoc />
    public bool Contribute( IActivityMonitor monitor, AppBuilderContext context )
    {
        bool success = true;
        foreach( var binPath in _appIdentity.Clients.Where( c => c.Type == "TypeScript" ).GroupBy( c => c.BinPath ) )
        {
            var multiple = new XElement( EngineConfiguration.xMultiple );
            foreach( var c in binPath )
            {
                var x = ToXml( monitor, c );
                if( x == null )
                {
                    success = false;
                    continue;
                }
                multiple.Add( x );
                foreach( var k in c.Configuration.GetChildren().Where( k => TypeScriptBinPathAspectConfiguration.ClientConfigurationKeys.Contains( k.Key ) ) )
                {
                    binPath.Key.AddCompileKey( $"{c.RelativePath}:{k.Key}" );
                }
                c.SetHandled();
            }
            if( !success ) continue;
            context.EngineConfiguration.EnsureAspect<TypeScriptAspectConfiguration>();
            var aspect = binPath.Key.BinPathConfiguration.EnsureAspect<TypeScriptBinPathAspectConfiguration>();
            try
            {
                aspect.InitializeFrom( new XElement( "TypeScript", multiple ) );
            }
            catch( System.Exception ex )
            {
                monitor.Error( $"Invalid TypeScript client configuration in {binPath.Key}.", ex );
                success = false;
            }
        }
        return success;
    }

    // The XML that a "CKSetup.xml" gives to the aspect: the scalar keys are attributes, "Types", "ExcludedTypes" and
    // "Barrels" are lists. The other keys are not this aspect's (the client's options).
    static XElement? ToXml( IActivityMonitor monitor, AppIdentityAppBuilderContributor.DeclaredClient c )
    {
        var x = new XElement( "TypeScript", new XAttribute( "Name", c.Name ) );
        foreach( var k in c.Configuration.GetChildren() )
        {
            if( !TypeScriptBinPathAspectConfiguration.ClientConfigurationKeys.Contains( k.Key ) ) continue;
            switch( k.Key.ToLowerInvariant() )
            {
                case "types":
                case "excludedtypes":
                    x.Add( new XElement( k.Key.Equals( "Types", System.StringComparison.OrdinalIgnoreCase ) ? EngineConfiguration.xTypes : EngineConfiguration.xExcludedTypes,
                                         k.GetChildren().Select( t => ToTypeXml( t ) ) ) );
                    break;
                case "barrels":
                    x.Add( new XElement( "Barrels", k.GetChildren().Select( b => new XElement( "Barrel", new XAttribute( EngineConfiguration.xPath, b.Value ?? b["Path"] ?? "" ) ) ) ) );
                    break;
                case "activecultures":
                    // A comma separated string or an array.
                    x.Add( new XAttribute( "ActiveCultures", k.Value ?? string.Join( ',', k.GetChildren().Select( v => v.Value ) ) ) );
                    break;
                default:
                    if( k.Value == null )
                    {
                        monitor.Error( $"\"{k.Path}\" must be a value." );
                        return null;
                    }
                    x.Add( new XAttribute( k.Key, k.Value ) );
                    break;
            }
        }
        return x;

        // A type is its assembly qualified name, or an object whose keys are its attributes ({ "Name": "...", "Optional": true }).
        static XElement ToTypeXml( ImmutableConfigurationSection t )
        {
            if( t.Value != null ) return new XElement( EngineConfiguration.xType, t.Value );
            return new XElement( EngineConfiguration.xType, t.GetChildren().Where( a => a.Value != null ).Select( a => new XAttribute( a.Key, a.Value! ) ) );
        }
    }
}
