using CK.Core;
using CK.TypeScript.CodeGen;
using System.Collections.Generic;
using CSemVer;
using System.Diagnostics.CodeAnalysis;
using System;
using System.Collections.Immutable;

namespace CK.Setup;

public sealed partial class TypeScriptIntegrationContext
{
    public class BaseEventArgs : EventMonitoredArgs
    {
        readonly NormalizedPath _yarnPath;

        internal BaseEventArgs( IActivityMonitor monitor,
                                TypeScriptIntegrationContext integrationContext,
                                NormalizedPath yarnPath )
            : base( monitor )
        {
            IntegrationContext = integrationContext;
            _yarnPath = yarnPath;
        }

        /// <summary>
        /// Gets the <see cref="TypeScriptIntegrationContext"/>.
        /// </summary>
        public TypeScriptIntegrationContext IntegrationContext { get; }

        /// <summary>
        /// Gets the <see cref="TypeScriptBinPathAspectConfiguration.TargetProjectPath"/>.
        /// </summary>
        public NormalizedPath TargetProjectPath => IntegrationContext.Configuration.TargetProjectPath;

        /// <summary>
        /// Gets the configured LibraryVersions.
        /// </summary>
        public ImmutableDictionary<string, SVersionBound> ConfiguredLibraries => IntegrationContext._libVersionsConfig;

        /// <summary>
        /// Gets or sets the <see cref="JestSetupHandler"/> to use.
        /// <para>
        /// This is null and must remain null when <see cref="TypeScriptBinPathAspectConfiguration.AutoInstallJest"/> is false.
        /// </para>
        /// </summary>
        [DisallowNull]
        public JestSetupHandler? JestSetup
        {
            get => IntegrationContext._jestSetup;
            set
            {
                Throw.CheckState( IntegrationContext.Configuration.AutoInstallJest is true );
                Throw.CheckNotNullArgument( value );
                IntegrationContext._jestSetup = value;
            }
        }

        /// <summary>
        /// Calls Yarn with the provided <paramref name="command"/>.
        /// The monitor used is this <see cref="EventMonitoredArgs.Monitor"/>.
        /// </summary>
        /// <param name="command">The command to run.</param>
        /// <param name="environmentVariables">Optional environment variables to set.</param>
        /// <param name="workingDirectory">Optional working directory. Deadults to <see cref="TypeScriptBinPathAspectConfiguration.TargetProjectPath"/>.</param>
        /// <returns>True on success, false if the process failed.</returns>
        public bool RunYarn( string command, Dictionary<string, string>? environmentVariables = null, NormalizedPath workingDirectory = default )
        {
            if( workingDirectory.IsEmptyPath ) workingDirectory = IntegrationContext.Configuration.TargetProjectPath;
            return YarnHelper.DoRunYarn( Monitor, workingDirectory, command, _yarnPath, environmentVariables );
        }

        /// <summary>
        /// Executes a command.
        /// </summary>
        /// <param name="fileName">The process file name.</param>
        /// <param name="arguments">The arguments.</param>
        /// <param name="environmentVariables">Optional environment variables.</param>
        /// <param name="workingDirectory">Optional working directory. Defaults to <see cref="TargetProjectPath"/>.</param>
        /// <returns>The process exit code.</returns>
        public int RunProcess( string fileName,
                               string arguments,
                               Dictionary<string, string>? environmentVariables = null,
                               NormalizedPath workingDirectory = default )
        {
            if( workingDirectory.IsEmptyPath ) workingDirectory = IntegrationContext.Configuration.TargetProjectPath;
            return YarnHelper.RunProcess( Monitor.ParallelLogger, fileName, arguments, workingDirectory, environmentVariables );
        }
    }

    /// <summary>
    /// Event argument of <see cref="OnBeforeIntegration"/>.
    /// </summary>
    public sealed class BeforeEventArgs : BaseEventArgs
    {
        internal BeforeEventArgs( IActivityMonitor monitor, TypeScriptIntegrationContext integrationContext, NormalizedPath yarnPath )
            : base( monitor, integrationContext, yarnPath )
        {
        }

        /// <summary>
        /// Consider the target project, the dependencies declared by the code, the Yarn Sdk version, the
        /// <see cref="TypeScriptBinPathAspectConfiguration.DefaultTypeScriptVersion"/> and the <see cref="DefaultTypeScriptVersion"/>
        /// in this order to resolve the TypeScript version.
        /// <para>
        /// If the <see cref="TargetPackageJson"/> doesn't contain the "typescript" depenendency, is is added, the package.json file is saved
        /// and the Yarn TypeScript Sdk (and the VSCode Sdk) is installed.
        /// </para>
        /// <para>
        /// This method is called automatically after this event if it has not been called.
        /// It should be called only if for any reason the TypeScript version must be known, for instance after having
        /// generated a target package.json (a valid <see cref="TargetPackageJson"/> file) through external tools.
        /// </para>
        /// <para>
        /// When called multiple times, the first computed result is returned as-is.
        /// </para>
        /// </summary>
        /// <param name="typeScriptDep">The package dependency on success.</param>
        /// <returns>True on success, false otherwise.</returns>
        public bool SettleTypeScriptVersion( [NotNullWhen( true )] out PackageDependency? typeScriptDependency ) => IntegrationContext.SettleTypeScriptVersion( Monitor, out typeScriptDependency );

        /// <summary>
        /// Adds or updates a <see cref="PackageJsonFile.Dependencies"/> in the <see cref="TargetPackageJson"/> taking care
        /// of a configured version in <see cref="TypeScriptAspectConfiguration.LibraryVersions"/>.
        /// </summary>
        /// <param name="name">The dependency name.</param>
        /// <param name="versionBound">The dependency version bound.</param>
        /// <param name="kind">The dependency kind.</param>
        /// <returns>True on success, false on error.</returns>
        public bool AddOrUpdateTargetProjectDependency( string name, string versionBound, DependencyKind kind )
        {
            Throw.CheckNotNullArgument( name );
            Throw.CheckNotNullArgument( versionBound );
            return LibraryManager.TryParseVersionBound( Monitor, name, versionBound, kind, out var v )
                   && IntegrationContext.AddOrUpdateTargetProjectDependency( Monitor, name, v, kind );
        }

        /// <summary>
        /// Adds or updates a <see cref="PackageJsonFile.Dependencies"/> in the <see cref="TargetPackageJson"/> taking care
        /// of a configured version in <see cref="TypeScriptAspectConfiguration.LibraryVersions"/>.
        /// </summary>
        /// <param name="name">The dependency name.</param>
        /// <param name="version">The dependency version.</param>
        /// <param name="kind">The dependency kind.</param>
        /// <returns>True on success, false otherwise.</returns>
        public bool AddOrUpdateTargetProjectDependency( string name, SVersionBound version, DependencyKind kind ) => IntegrationContext.AddOrUpdateTargetProjectDependency( Monitor, name, version, kind );

        /// <summary>
        /// Adds or updates a <see cref="PackageJsonFile.Dependencies"/> in the <see cref="TargetPackageJson"/> that 
        /// must be configured in <see cref="TypeScriptAspectConfiguration.LibraryVersions"/>.
        /// <para>
        /// This overload requires that the package and its version is explicitly configured in the Library versions.
        /// </para>
        /// </summary>
        /// <param name="name">The dependency name.</param>
        /// <param name="kind">The dependency kind.</param>
        /// <returns>True on success, false on error.</returns>
        public bool AddOrUpdateConfiguredTargetProjectDependency( string name, DependencyKind kind ) => IntegrationContext.AddOrUpdateTargetProjectDependency( Monitor, name, null, kind );

        /// <summary>
        /// Ensures that <see cref="TargetPackageJson"/> dependencies are installed. Calls <see cref="SettleTypeScriptVersion(out PackageDependency?)"/>,
        /// saves the package.json and runs the installation of the packages.
        /// </summary>
        /// <returns>True on success, false otherwise.</returns>
        bool RunYarnInstall() => IntegrationContext.SaveTargetPackageJsonAndYarnInstall( Monitor );

    }

    private bool AddOrUpdateTargetProjectDependency( IActivityMonitor monitor, string name, object version, DependencyKind kind )
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Event argument of <see cref="OnAfterIntegration"/>.
    /// To enable cleanup operations even on failure, this event is raised even if an error occurred (<see cref="Success"/> is false).
    /// </summary>
    public sealed class AfterEventArgs : BaseEventArgs
    {

        internal AfterEventArgs( IActivityMonitor monitor, TypeScriptIntegrationContext integrationContext, NormalizedPath yarnPath, bool success )
            : base( monitor, integrationContext, yarnPath )
        {
            Success = success;
        }

        /// <summary>
        /// Gets whether the integration succeeded.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets the /src folder. It necessarily exists.
        /// </summary>
        public NormalizedPath SrcFolderPath => IntegrationContext.SrcFolderPath;
    }

}
