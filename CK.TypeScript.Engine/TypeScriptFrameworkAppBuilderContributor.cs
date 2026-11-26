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
///   <item>The feature declares its compile time model: <c>[CompileConfiguration&lt;TModel&gt;]</c> where the model is
///   (or derives from) <see cref="TypeScriptClientCompileConfiguration"/>. The App builder reads it (its keys are compile
///   time keys: they are not projected in the host's "appsettings.json"), this contributor receives it
///   (<see cref="AppIdentityAppBuilderContributor.DeclaredClient.GetCompileModel{TModel}(string)"/>) and never reads the
///   section.</item>
///   <item>The "Types" and "ExcludedTypes" of the client are its type set: the TypeScript engine reads the computed set
///   (its TypeScript packages, the Pocos it lists). The TypeScript settings of its types are the
///   <see cref="TypeScriptClientCompileConfiguration.Types"/> of the model.</item>
///   <item>The <see cref="TypeScriptClientCompileConfiguration.RequestPath"/> ("/{kebab-name}" by default) is unique among
///   the clients of an App.</item>
///   <item>The presence of a client gives the <see cref="TypeScriptAspectConfiguration"/>.</item>
/// </list>
/// A package that brings a TypeScript framework specializes it and registers it with <see cref="AppBuilderContributorAttribute"/>.
/// </summary>
public abstract class TypeScriptFrameworkAppBuilderContributor : IAppBuilderContributor
{
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
        // A feature turned off ("NgApp": false) is not there.
        foreach( var app in _appIdentity.Clients.Where( c => c.Features.TryGetValue( FeatureName, out var f ) && !string.Equals( f.Value, "false", StringComparison.OrdinalIgnoreCase ) )
                                                .GroupBy( c => c.App ) )
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

    // The XML that a "CKSetup.xml" gives to the aspect, from the compile time model of the client's feature.
    XElement? ToXml( IActivityMonitor monitor, AppIdentityAppBuilderContributor.DeclaredClient c, Dictionary<string, string> requestPaths )
    {
        var m = c.GetCompileModel<TypeScriptClientCompileConfiguration>( FeatureName );
        if( m == null )
        {
            // An invalid declaration ("NgApp": "yes") has been reported by the AppIdentity contributor.
            if( c.Features[FeatureName].Value != null ) return null;
            monitor.Error( $"The client feature '{FeatureName}' of the {c} has no compile time model: it must declare [CompileConfiguration<TModel>] where the model is (or derives from) '{nameof( TypeScriptClientCompileConfiguration )}'." );
            return null;
        }
        var x = new XElement( "TypeScript", new XAttribute( "Name", c.Name ) );
        Add( "TargetProjectPath", m.TargetProjectPath );
        Add( "DefaultTypeScriptVersion", m.DefaultTypeScriptVersion );
        Add( "IntegrationMode", m.IntegrationMode?.ToString() );
        Add( "InstallYarn", m.InstallYarn?.ToString() );
        Add( "GitIgnoreCKGenFolder", m.GitIgnoreCKGenFolder?.ToString() );
        Add( "AutoInstallJest", m.AutoInstallJest?.ToString() );
        Add( "EnableTSProjectReferences", m.EnableTSProjectReferences?.ToString() );
        Add( "DefaultCulture", m.DefaultCulture );
        Add( "ActiveCultures", m.ActiveCultures != null ? string.Join( ',', m.ActiveCultures ) : null );
        Add( "TypeFilterName", m.TypeFilterName );
        if( m.Barrels != null )
        {
            x.Add( new XElement( "Barrels", m.Barrels.Select( b => new XElement( "Barrel", new XAttribute( EngineConfiguration.xPath, b ) ) ) ) );
        }
        if( m.Types != null && m.Types.Count > 0 )
        {
            x.Add( new XElement( EngineConfiguration.xTypes,
                                 m.Types.Select( t => new XElement( EngineConfiguration.xType,
                                                                    new XAttribute( EngineConfiguration.xName, t.Name ),
                                                                    Attr( "TypeName", t.TypeName ),
                                                                    Attr( "Folder", t.Folder ),
                                                                    Attr( "FileName", t.FileName ),
                                                                    Attr( "SameFileAs", t.SameFileAs ),
                                                                    Attr( "SameFolderAs", t.SameFolderAs ) ) ) ) );
        }
        bool success = true;
        var requestPath = m.RequestPath;
        if( requestPath != null )
        {
            if( !requestPath.StartsWith( '/' ) )
            {
                monitor.Error( $"Invalid \"{c.Configuration.Path}:{FeatureName}:RequestPath\": '{requestPath}' must start with a '/'." );
                success = false;
            }
            else
            {
                x.Add( new XAttribute( "RequestPath", requestPath ) );
            }
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
        if( m.DevServerPort is int port ) x.Add( new XAttribute( "DevServerPort", port ) );
        return success ? x : null;

        void Add( string name, string? value )
        {
            if( value != null ) x.Add( new XAttribute( name, value ) );
        }

        static XAttribute? Attr( string name, string? value ) => value != null ? new XAttribute( name, value ) : null;
    }
}
