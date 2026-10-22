using CK.TypeScript.CodeGen;
using CSemVer;
using System.Collections.Generic;
using System.Linq;

namespace CK.TS.Angular.Engine;

/// <summary>
/// A short text shown when a tooling row is crossed, optionally tied to a compile error.
/// A compile error line matches when it contains every match value that is set; a hint
/// with neither <paramref name="MatchFragment"/> nor <paramref name="MatchErrorCode"/> never matches a line.
/// </summary>
/// <param name="Text">The hint text.</param>
/// <param name="MatchFragment">Optional text fragment, such as <c>NzToolTipModule</c>.</param>
/// <param name="MatchErrorCode">Optional compiler error code, such as <c>NG8023</c>.</param>
internal sealed record AngularUpgradeHint( string Text, string? MatchFragment = null, string? MatchErrorCode = null );

/// <summary>
/// What the engine needs to reach one Angular major.
/// </summary>
/// <param name="Major">The Angular major this row reaches.</param>
/// <param name="PreUpdateBounds">
/// Packages set in package.json before the <c>ng update</c> of this major, each only when the app already has it:
/// an older version blocks <c>ng update</c> with incompatible peers.
/// </param>
/// <param name="PlatformBrowser">The <c>@angular/platform-browser</c> bound injected before integration.</param>
/// <param name="NodeRange">The Node versions accepted by this major's CLI: satisfied when any bound contains the version.</param>
/// <param name="ReplacedPackages">
/// <c>@angular/*</c> packages replaced in this major, with their replacement. <c>ng update</c> bumps
/// the replaced package and keeps it, so the replacement is done after the migrations.
/// </param>
/// <param name="Hints">Hints shown when this row is crossed.</param>
internal sealed record AngularMajorTooling( int Major,
                                            IReadOnlyList<(string Name, SVersionBound Version, DependencyKind Kind)> PreUpdateBounds,
                                            SVersionBound PlatformBrowser,
                                            IReadOnlyList<SVersionBound> NodeRange,
                                            IReadOnlyList<(string Package, string Replacement)> ReplacedPackages,
                                            IReadOnlyList<AngularUpgradeHint> Hints )
{
    /// <summary>
    /// Gets the known rows, indexed by <see cref="Major"/>.
    /// </summary>
    internal static IReadOnlyDictionary<int, AngularMajorTooling> Rows { get; } = CreateRows();

    /// <summary>
    /// Gets the lowest major of <see cref="Rows"/>.
    /// </summary>
    internal static int FirstMajor { get; } = Rows.Keys.Min();

    static Dictionary<int, AngularMajorTooling> CreateRows()
    {
        // @angular/core 21.0.0 peers zone.js on "~0.15.0" and 22.0.0 on "~0.15.0 || ~0.16.0": "~0.15.0" fits both,
        // and one SVersionBound cannot hold the union.
        (string, SVersionBound, DependencyKind)[] preUpdateBounds = [("jest-preset-angular", B( "^17.0.1" ), DependencyKind.DevDependency),
                                                                    ("zone.js", B( "~0.15.0" ), DependencyKind.Dependency)];
        var rows = new AngularMajorTooling[]
        {
            new AngularMajorTooling( 21,
                                     preUpdateBounds,
                                     B( "^21.0.0" ),
                                     [B( "^20.19.0" ), B( "^22.12.0" ), B( ">=24.0.0" )],
                                     [],
                                     [new AngularUpgradeHint( "The control-flow-migration always runs in Angular 21: every template is converted to @if and @for, expect a large diff." )] ),
            new AngularMajorTooling( 22,
                                     preUpdateBounds,
                                     B( "^22.0.0" ),
                                     [B( "^22.22.3" ), B( "^24.15.0" ), B( ">=26.0.0" )],
                                     [("@angular/platform-browser-dynamic", "@angular/platform-browser")],
                                     [new AngularUpgradeHint( "ng-zorro-antd 22 renames NzToolTipModule to NzTooltipModule: update the imports.", MatchFragment: "NzToolTipModule" ),
                                      new AngularUpgradeHint( "@angular/animations is deprecated in Angular 22." ),
                                      new AngularUpgradeHint( "The change-detection-eager and strict-templates-default migrations touch many files." )] )
        };
        return rows.ToDictionary( r => r.Major );
    }

    static SVersionBound B( string npm ) => SVersionBound.NpmTryParse( npm ).Result;
}
