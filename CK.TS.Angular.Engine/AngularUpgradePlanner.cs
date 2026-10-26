using CK.TypeScript.CodeGen;
using CSemVer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.TS.Angular.Engine;

/// <summary>
/// A package updated by an <see cref="AngularUpgradeStep"/>.
/// </summary>
/// <param name="Name">The package name.</param>
/// <param name="FromMajor">The package major before the step.</param>
/// <param name="ToMajor">The package major the step updates to.</param>
internal sealed record AngularUpgradePackage( string Name, int FromMajor, int ToMajor );

/// <summary>
/// One Angular major crossed by an upgrade.
/// </summary>
/// <param name="FromMajor">The Angular major before the step.</param>
/// <param name="ToMajor">The Angular major after the step.</param>
/// <param name="Tooling">The tooling row of <paramref name="ToMajor"/>.</param>
/// <param name="Packages">
/// The packages of the package command: <c>@angular/core</c>, <c>@angular/cli</c>, the same-major libraries,
/// then the CK-declared libraries (last step only).
/// </param>
/// <param name="PreUpdateBounds">The row's pre-update bounds of the packages the app has.</param>
/// <param name="ReplacedPackages">The row's replaced packages that the app has.</param>
/// <param name="PinnedGroupPackages">
/// The <c>@angular/core</c> package group members the app has that the package command names at <paramref name="ToMajor"/>:
/// otherwise the CLI takes the target of an unnamed package from the first peer range that claims it, and <c>@angular/cdk</c>
/// accepts the next major for <c>@angular/common</c>, which drags the whole group one major too far. They have no migrations
/// of their own, so they are not <see cref="Packages"/>.
/// </param>
internal sealed record AngularUpgradeStep( int FromMajor,
                                           int ToMajor,
                                           AngularMajorTooling Tooling,
                                           IReadOnlyList<AngularUpgradePackage> Packages,
                                           IReadOnlyList<(string Name, SVersionBound Version, DependencyKind Kind)> PreUpdateBounds,
                                           IReadOnlyList<(string Package, string Replacement)> ReplacedPackages,
                                           IReadOnlyList<string> PinnedGroupPackages )
{
    /// <summary>
    /// Gets the <c>ng update</c> command that updates every <see cref="Packages"/> and <see cref="PinnedGroupPackages"/>
    /// (without the <c>yarn </c> prefix).
    /// </summary>
    internal string PackageCommand => $"ng update {string.Join( " ", Packages.Select( p => $"{p.Name}@{p.ToMajor}" ).Concat( PinnedGroupPackages.Select( n => $"{n}@{ToMajor}" ) ) )} --allow-dirty";

    /// <summary>
    /// Gets the <c>ng update --migrate-only</c> command of a package (without the <c>yarn </c> prefix).
    /// </summary>
    /// <param name="package">The package.</param>
    /// <param name="from">
    /// The exact version installed before the package command: the CLI runs the migrations in <c>(from, to]</c>,
    /// so a bare major would run again the migrations of the old major.
    /// </param>
    /// <returns>The migration command.</returns>
    internal string MigrationCommand( AngularUpgradePackage package, string from ) => $"ng update {package.Name} --migrate-only --from {from} --to {package.ToMajor} --allow-dirty";
}

/// <summary>
/// Computes the steps of an Angular major upgrade. Pure: no I/O.
/// </summary>
internal static class AngularUpgradePlanner
{
    /// <summary>
    /// Libraries that follow Angular's major numbering: updated at each step when the app has them.
    /// </summary>
    static readonly string[] _sameMajorLibraries = ["ng-zorro-antd", "@angular/cdk"];

    /// <summary>
    /// The members of the <c>packageGroup</c> that <c>@angular/core</c> declares in its <c>ng-update</c> metadata (Angular 21
    /// and 22), without <c>@angular/core</c> itself and without <c>@angular/animations</c> and <c>@angular/platform-browser-dynamic</c>:
    /// every release of these two is deprecated, and the CLI refuses a command line range that only matches deprecated
    /// releases. The group update of <c>@angular/common</c> still moves them to the step's major.
    /// </summary>
    static readonly string[] _pinnedCoreGroup = ["@angular/common", "@angular/compiler", "@angular/compiler-cli",
                                                 "@angular/elements", "@angular/forms", "@angular/language-service",
                                                 "@angular/localize", "@angular/platform-browser", "@angular/platform-server",
                                                 "@angular/router", "@angular/service-worker", "@angular/upgrade"];

    /// <summary>
    /// Gets the major of the app's <c>@angular/core</c>.
    /// </summary>
    /// <param name="appDependencies">The app's dependencies.</param>
    /// <returns>The major, or null when <c>@angular/core</c> is missing or has no major (<see cref="SVersionBound.All"/> or <see cref="SVersionBound.None"/>).</returns>
    internal static int? GetAppMajor( IReadOnlyDictionary<string, SVersionBound> appDependencies )
    {
        return appDependencies.TryGetValue( "@angular/core", out var core ) && HasMajor( core ) ? core.Base.Major : null;
    }

    /// <summary>
    /// Plans one step per major from <paramref name="appMajor"/> + 1 to <paramref name="targetMajor"/>.
    /// </summary>
    /// <param name="appMajor">The app's Angular major.</param>
    /// <param name="targetMajor">The Angular major to reach.</param>
    /// <param name="appDependencies">The app's dependencies.</param>
    /// <param name="declaredLibraries">The CK-declared library bounds.</param>
    /// <returns>The steps, empty when <paramref name="appMajor"/> is at or above <paramref name="targetMajor"/>.</returns>
    /// <exception cref="ArgumentException">A crossed major has no <see cref="AngularMajorTooling.Rows"/> entry.</exception>
    internal static IReadOnlyList<AngularUpgradeStep> Plan( int appMajor,
                                                            int targetMajor,
                                                            IReadOnlyDictionary<string, SVersionBound> appDependencies,
                                                            IReadOnlyDictionary<string, SVersionBound> declaredLibraries )
    {
        var steps = new List<AngularUpgradeStep>();
        var pinned = _pinnedCoreGroup.Where( appDependencies.ContainsKey ).ToList();
        for( int to = appMajor + 1; to <= targetMajor; to++ )
        {
            if( !AngularMajorTooling.Rows.TryGetValue( to, out var row ) )
            {
                throw new ArgumentException( $"No Angular tooling row for major {to}: the safety check refuses this path before any plan is made.", nameof( targetMajor ) );
            }
            var packages = new List<AngularUpgradePackage> { new( "@angular/core", to - 1, to ), new( "@angular/cli", to - 1, to ) };
            packages.AddRange( _sameMajorLibraries.Where( appDependencies.ContainsKey ).Select( name => new AngularUpgradePackage( name, to - 1, to ) ) );
            if( to == targetMajor )
            {
                packages.AddRange( GetLastStepLibraries( appDependencies, declaredLibraries ) );
            }
            steps.Add( new AngularUpgradeStep( to - 1,
                                               to,
                                               row,
                                               packages,
                                               row.PreUpdateBounds.Where( b => appDependencies.ContainsKey( b.Name ) ).ToList(),
                                               row.ReplacedPackages.Where( r => appDependencies.ContainsKey( r.Package ) ).ToList(),
                                               pinned ) );
        }
        return steps;
    }

    /// <summary>
    /// Gets the CK-declared libraries the last step updates: present in the app, neither <c>@angular/*</c> nor a same-major
    /// library, and declared with a major above the app's major of that library. Same-major minor gaps and libraries
    /// where the app is ahead are left to the package.json merge.
    /// </summary>
    /// <param name="appDependencies">The app's dependencies.</param>
    /// <param name="declaredLibraries">The CK-declared library bounds.</param>
    /// <returns>The libraries, ordered by name.</returns>
    internal static IReadOnlyList<AngularUpgradePackage> GetLastStepLibraries( IReadOnlyDictionary<string, SVersionBound> appDependencies,
                                                                               IReadOnlyDictionary<string, SVersionBound> declaredLibraries )
    {
        var result = new List<AngularUpgradePackage>();
        foreach( var (name, declared) in declaredLibraries.OrderBy( d => d.Key, StringComparer.Ordinal ) )
        {
            if( name.StartsWith( "@angular/", StringComparison.Ordinal )
                || _sameMajorLibraries.Contains( name )
                || !HasMajor( declared )
                || !appDependencies.TryGetValue( name, out var current )
                || !HasMajor( current ) )
            {
                continue;
            }
            if( declared.Base.Major > current.Base.Major )
            {
                result.Add( new AngularUpgradePackage( name, current.Base.Major, declared.Base.Major ) );
            }
        }
        return result;
    }

    /// <summary>
    /// Renders the steps as a PowerShell script a developer runs in the app folder, in the order the engine runs them.
    /// A package's <c>--from</c> comes from <paramref name="installedVersions"/> until a step updates it; after that,
    /// the script names the version the previous <c>ng update</c> printed, which only exists once that command ran.
    /// </summary>
    /// <param name="steps">The steps from <see cref="Plan"/>.</param>
    /// <param name="installedVersions">The versions installed before the first step, by package name.</param>
    /// <returns>The script.</returns>
    internal static string RenderManualCommands( IReadOnlyList<AngularUpgradeStep> steps, IReadOnlyDictionary<string, SVersion> installedVersions )
    {
        var b = new StringBuilder();
        b.AppendLine( "# Run in the app folder, in PowerShell, one step after the other." );
        b.AppendLine( "$env:NG_DISABLE_VERSION_CHECK = \"1\"" );
        b.AppendLine( "$env:NG_FORCE_TTY = \"0\"" );
        b.AppendLine( "$env:NO_COLOR = \"1\"" );
        b.AppendLine( "Remove-Item Env:FORCE_COLOR -ErrorAction SilentlyContinue" );
        b.AppendLine( "Remove-Item Env:YARN_PNP_MODE -ErrorAction SilentlyContinue" );
        for( int i = 0; i < steps.Count; i++ )
        {
            var step = steps[i];
            b.AppendLine();
            b.AppendLine( $"# Angular {step.FromMajor} to {step.ToMajor}." );
            if( step.PreUpdateBounds.Count > 0 )
            {
                b.AppendLine( "# In package.json, set:" );
                foreach( var (name, version, kind) in step.PreUpdateBounds )
                {
                    b.AppendLine( $"#   \"{name}\": \"{ToPackageJsonRange( version )}\" (in \"{kind.GetJsonSectionName()}\")" );
                }
                b.AppendLine( "yarn install" );
            }
            b.AppendLine( "# This may crash in its migration phase (Cannot find module '@angular-devkit/core')." );
            b.AppendLine( $"# Carry on when package.json shows @angular/core at major {step.ToMajor}." );
            b.AppendLine( "yarn " + step.PackageCommand );
            b.AppendLine( "yarn dedupe" );
            // The migrations load schematics packages that a strict PnP install does not expose to them.
            b.AppendLine( "$env:YARN_PNP_MODE = \"loose\"" );
            b.AppendLine( "yarn install" );
            b.AppendLine( "# A package without migrations exits 1 with \"Package does not provide migrations.\": this is harmless." );
            foreach( var package in GetMigrationOrder( step ) )
            {
                bool updatedBefore = steps.Take( i ).Any( s => s.Packages.Any( p => p.Name == package.Name ) );
                string from = updatedBefore
                                ? $"<version printed by the previous ng update for {package.Name}>"
                                : installedVersions.TryGetValue( package.Name, out var installed )
                                    ? installed.ToString()
                                    : $"<version of {package.Name} in yarn.lock>";
                b.AppendLine( "yarn " + step.MigrationCommand( package, from ) );
            }
            b.AppendLine( "Remove-Item Env:YARN_PNP_MODE" );
            foreach( var (package, replacement) in step.ReplacedPackages )
            {
                b.AppendLine( $"yarn remove {package}" );
                b.AppendLine( $"# When package.json has no \"{replacement}\": yarn add {replacement}@^{step.ToMajor}.0.0" );
            }
        }
        return b.ToString();
    }

    internal static IEnumerable<AngularUpgradePackage> GetMigrationOrder( AngularUpgradeStep step )
    {
        return step.Packages.Where( p => p.Name == "@angular/cli" )
                            .Concat( step.Packages.Where( p => p.Name == "@angular/core" ) )
                            .Concat( step.Packages.Where( p => p.Name != "@angular/cli" && p.Name != "@angular/core" ) );
    }

    static bool HasMajor( SVersionBound bound ) => bound != SVersionBound.None && bound.NormalizeNpmVersionBoundAll() != SVersionBound.All;

    // ToNpmString shortens "~0.15.0" to "~0.15": this keeps the three parts package.json files usually hold.
    static string ToPackageJsonRange( SVersionBound bound ) => bound.Lock switch
    {
        SVersionLock.Lock => bound.Base.ToString(),
        SVersionLock.LockMinor => "~" + bound.Base.ToString(),
        SVersionLock.LockMajor when bound.Base.Major > 0 => "^" + bound.Base.ToString(),
        _ => bound.ToNpmString()
    };
}
