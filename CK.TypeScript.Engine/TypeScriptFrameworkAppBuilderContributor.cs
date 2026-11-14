using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace CK.Setup;

/// <summary>
/// Configures the TypeScript aspect from the clients declared by the parties of the Apps ("ckonf.json") that have the
/// client feature of a TypeScript framework (its <see cref="FeatureName"/>: "NgApp" for an Angular application): such a
/// client is a <see cref="TypeScriptBinPathAspectConfiguration"/> of its App, named after the client.
/// <list type="bullet">
///   <item>The keys of the framework section (<see cref="TypeScriptBinPathAspectConfiguration.ClientConfigurationKeys"/>)
///   configure it; the "Types" and "ExcludedTypes" of the client (its type set) are its types.</item>
///   <item>Its "Hosting" section (the client served by its party) gives its <see cref="TypeScriptBinPathAspectConfiguration.RequestPath"/>
///   (optional: "/{kebab-name}" by default, "/" for the default client) and its <see cref="TypeScriptBinPathAspectConfiguration.DevServerPort"/>.
///   A client without "Hosting" is not supported yet.</item>
///   <item>These keys are compile time keys: they are not projected in the host's "appsettings.json". The other keys of
///   "Hosting" are the run time's. The framework section and "Hosting" stay declared in the projection (written
///   <c>true</c> when all their keys are compile time keys, see <see cref="AppConfiguration.AddDeclarationKey(string)"/>).</item>
///   <item>The presence of a client gives the <see cref="TypeScriptAspectConfiguration"/>.</item>
/// </list>
/// A package that brings a TypeScript framework specializes it and registers it with <see cref="AppBuilderContributorAttribute"/>.
/// </summary>
public abstract class TypeScriptFrameworkAppBuilderContributor : IAppBuilderContributor
{
    /// <summary>
    /// The name of the client feature that serves a client: "Hosting".
    /// </summary>
    public const string HostingFeatureName = "Hosting";

    readonly AppIdentityAppBuilderContributor _appIdentity;

    /// <summary>
    /// Initializes a new contributor.
    /// </summary>
    /// <param name="appIdentity">The parties and their declared clients.</param>
    /// <param name="featureName">The name of the client feature of the framework.</param>
    protected TypeScriptFrameworkAppBuilderContributor( AppIdentityAppBuilderContributor appIdentity, string featureName )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( featureName );
        _appIdentity = appIdentity;
        FeatureName = featureName;
    }

    /// <summary>
    /// Gets the name of the client feature of the framework ("NgApp").
    /// </summary>
    public string FeatureName { get; }

    /// <inheritdoc />
    public bool Contribute( IActivityMonitor monitor, AppBuilderContext context )
    {
        bool success = true;
        foreach( var app in _appIdentity.Clients.Where( c => c.Features.ContainsKey( FeatureName ) ).GroupBy( c => c.App ) )
        {
            var multiple = new XElement( EngineConfiguration.xMultiple );
            var requestPaths = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );
            foreach( var c in app )
            {
                if( !c.SetHandled( FeatureName ) )
                {
                    monitor.Error( $"The {c} has two build features: '{c.BuildFeature}' and '{FeatureName}'." );
                    success = false;
                    continue;
                }
                var x = ToXml( monitor, c, requestPaths );
                if( x == null )
                {
                    success = false;
                    continue;
                }
                multiple.Add( x );
            }
            if( !success ) continue;
            context.EngineConfiguration.EnsureAspect<TypeScriptAspectConfiguration>();
            var aspect = app.Key.BinPathConfiguration.EnsureAspect<TypeScriptBinPathAspectConfiguration>();
            try
            {
                aspect.InitializeFrom( new XElement( "TypeScript", multiple ) );
            }
            catch( Exception ex )
            {
                monitor.Error( $"Invalid TypeScript client configuration in {app.Key}.", ex );
                success = false;
            }
        }
        return success;
    }

    /// <inheritdoc />
    public bool OnContributed( IActivityMonitor monitor, AppBuilderContext context ) => true;

    // The XML that a "CKSetup.xml" gives to the aspect: the scalar keys are attributes, "Types", "ExcludedTypes" and
    // "Barrels" are lists. Adds the compile time keys to the App.
    XElement? ToXml( IActivityMonitor monitor, AppIdentityAppBuilderContributor.DeclaredClient c, Dictionary<string, string> requestPaths )
    {
        var x = new XElement( "TypeScript", new XAttribute( "Name", c.Name ) );
        var framework = c.Features[FeatureName];
        bool success = true;
        if( framework.Value != null && !(bool.TryParse( framework.Value, out var declared ) && declared) )
        {
            monitor.Error( $"Invalid \"{framework.Path}\" = '{framework.Value}': a client feature is declared by true or by its section." );
            return null;
        }
        foreach( var k in framework.GetChildren() )
        {
            if( !TypeScriptBinPathAspectConfiguration.ClientConfigurationKeys.Contains( k.Key ) )
            {
                monitor.Error( $"Unknown \"{k.Path}\": the keys of \"{FeatureName}\" are '{TypeScriptBinPathAspectConfiguration.ClientConfigurationKeys.Order().Concatenate( "', '" )}' (the type set of the client is in its \"Types\" and \"ExcludedTypes\")." );
                success = false;
                continue;
            }
            c.App.AddCompileKey( $"{c.RelativePath}:{FeatureName}:{k.Key}" );
            switch( k.Key.ToLowerInvariant() )
            {
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
                        success = false;
                        continue;
                    }
                    x.Add( new XAttribute( k.Key, k.Value ) );
                    break;
            }
        }
        // The run time needs the feature (its build output): its emptied section is written true.
        c.App.AddDeclarationKey( $"{c.RelativePath}:{FeatureName}" );
        // The type set of the client: its Types and ExcludedTypes (the AppIdentity contributor made them compile keys).
        foreach( var key in (string[])["Types", "ExcludedTypes"] )
        {
            var list = c.Configuration.TryGetSection( key );
            if( list == null ) continue;
            var items = list.Value != null ? new[] { list } : list.GetChildren();
            x.Add( new XElement( key == "Types" ? EngineConfiguration.xTypes : EngineConfiguration.xExcludedTypes, items.Select( ToTypeXml ) ) );
        }
        // The hosting: its compile time keys.
        if( !c.Features.TryGetValue( HostingFeatureName, out var hosting ) )
        {
            monitor.Error( $"The {c} has no \"{HostingFeatureName}\": a {FeatureName} client that its party doesn't serve is not supported yet." );
            return null;
        }
        c.App.AddDeclarationKey( $"{c.RelativePath}:{HostingFeatureName}" );
        var requestPath = hosting["RequestPath"];
        if( requestPath != null )
        {
            if( !requestPath.StartsWith( '/' ) )
            {
                monitor.Error( $"Invalid \"{hosting.Path}:RequestPath\": '{requestPath}' must start with a '/'." );
                success = false;
            }
            else
            {
                x.Add( new XAttribute( "RequestPath", requestPath ) );
            }
            c.App.AddCompileKey( $"{c.RelativePath}:{HostingFeatureName}:RequestPath" );
        }
        else
        {
            x.Add( new XAttribute( "Kind", "App" ) );
        }
        var effectivePath = requestPath ?? "/" + System.Text.Json.JsonNamingPolicy.KebabCaseLower.ConvertName( c.Name );
        if( !requestPaths.TryAdd( effectivePath, c.Name ) )
        {
            monitor.Error( $"The clients '{requestPaths[effectivePath]}' and '{c.Name}' of {c.App} have the same request path '{effectivePath}'." );
            success = false;
        }
        var port = hosting["DevServerPort"];
        if( port != null )
        {
            x.Add( new XAttribute( "DevServerPort", port ) );
            c.App.AddCompileKey( $"{c.RelativePath}:{HostingFeatureName}:DevServerPort" );
        }
        return success ? x : null;

        // A type is its assembly qualified name, or an object whose keys are its attributes ({ "Name": "...", "Optional": true }).
        static XElement ToTypeXml( ImmutableConfigurationSection t )
        {
            if( t.Value != null ) return new XElement( EngineConfiguration.xType, t.Value );
            return new XElement( EngineConfiguration.xType, t.GetChildren().Where( a => a.Value != null ).Select( a => new XAttribute( a.Key, a.Value! ) ) );
        }
    }
}
