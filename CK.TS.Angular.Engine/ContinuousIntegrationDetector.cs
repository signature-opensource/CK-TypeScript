using System;
using System.Linq;

namespace CK.TS.Angular.Engine;

/// <summary>
/// Detects a CI run with the signals of Yarn 4.18.1's <c>isCI</c>, which is also the default of its
/// <c>enableImmutableInstalls</c> setting: by default, Yarn refuses to change the lockfile in a run detected here.
/// </summary>
internal static class ContinuousIntegrationDetector
{
    /// <summary>
    /// A known CI vendor: it is detected when all its conditions hold.
    /// </summary>
    /// <param name="Name">The vendor name.</param>
    /// <param name="Conditions">The conditions on the environment variables.</param>
    sealed record Vendor( string Name, Func<Func<string, string?>, bool>[] Conditions );

    /// <summary>
    /// The variables of which any non-empty one marks a CI run, in the order Yarn tests them.
    /// </summary>
    static readonly string[] _genericVariables = ["BUILD_ID",
                                                  "BUILD_NUMBER",
                                                  "CI",
                                                  "CI_APP_ID",
                                                  "CI_BUILD_ID",
                                                  "CI_BUILD_NUMBER",
                                                  "CI_NAME",
                                                  "CONTINUOUS_INTEGRATION",
                                                  "RUN_ID"];

    /// <summary>
    /// The vendor table of the ci-info copy in <c>.yarn/releases/yarn-4.18.1.cjs</c> (module <c>cue</c>, read by
    /// module <c>ly</c>), in its order, generated from that file. Its <c>pr</c> fields only tell a pull request
    /// build and are left out.
    /// </summary>
    static readonly Vendor[] _vendors =
        [
            new( "Agola CI", [Set( "AGOLA_GIT_REF" )] ),
            new( "Appcircle", [Set( "AC_APPCIRCLE" )] ),
            new( "AppVeyor", [Set( "APPVEYOR" )] ),
            new( "AWS CodeBuild", [Set( "CODEBUILD_BUILD_ARN" )] ),
            new( "Azure Pipelines", [Set( "TF_BUILD" )] ),
            new( "Bamboo", [Set( "bamboo_planKey" )] ),
            new( "Bitbucket Pipelines", [Set( "BITBUCKET_COMMIT" )] ),
            new( "Bitrise", [Set( "BITRISE_IO" )] ),
            new( "Buddy", [Set( "BUDDY_WORKSPACE_ID" )] ),
            new( "Buildkite", [Set( "BUILDKITE" )] ),
            new( "CircleCI", [Set( "CIRCLECI" )] ),
            new( "Cirrus CI", [Set( "CIRRUS_CI" )] ),
            new( "Codefresh", [Set( "CF_BUILD_ID" )] ),
            new( "Codemagic", [Set( "CM_BUILD_ID" )] ),
            new( "Codeship", [Equal( ("CI_NAME", "codeship") )] ),
            new( "Drone", [Set( "DRONE" )] ),
            new( "dsari", [Set( "DSARI" )] ),
            new( "Earthly", [Set( "EARTHLY_CI" )] ),
            new( "Expo Application Services", [Set( "EAS_BUILD" )] ),
            new( "Gerrit", [Set( "GERRIT_PROJECT" )] ),
            new( "Gitea Actions", [Set( "GITEA_ACTIONS" )] ),
            new( "GitHub Actions", [Set( "GITHUB_ACTIONS" )] ),
            new( "GitLab CI", [Set( "GITLAB_CI" )] ),
            new( "GoCD", [Set( "GO_PIPELINE_LABEL" )] ),
            new( "Google Cloud Build", [Set( "BUILDER_OUTPUT" )] ),
            new( "Harness CI", [Set( "HARNESS_BUILD_ID" )] ),
            new( "Heroku", [Includes( "NODE", "/app/.heroku/node/bin/node" )] ),
            new( "Hudson", [Set( "HUDSON_URL" )] ),
            new( "Jenkins", [Set( "JENKINS_URL" ), Set( "BUILD_ID" )] ),
            new( "LayerCI", [Set( "LAYERCI" )] ),
            new( "Magnum CI", [Set( "MAGNUM" )] ),
            new( "Netlify CI", [Set( "NETLIFY" )] ),
            new( "Nevercode", [Set( "NEVERCODE" )] ),
            new( "Prow", [Set( "PROW_JOB_ID" )] ),
            new( "ReleaseHub", [Set( "RELEASE_BUILD_ID" )] ),
            new( "Render", [Set( "RENDER" )] ),
            new( "Sail CI", [Set( "SAILCI" )] ),
            new( "Screwdriver", [Set( "SCREWDRIVER" )] ),
            new( "Semaphore", [Set( "SEMAPHORE" )] ),
            new( "Sourcehut", [Equal( ("CI_NAME", "sourcehut") )] ),
            new( "Strider CD", [Set( "STRIDER" )] ),
            new( "TaskCluster", [Set( "TASK_ID" ), Set( "RUN_ID" )] ),
            new( "TeamCity", [Set( "TEAMCITY_VERSION" )] ),
            new( "Travis CI", [Set( "TRAVIS" )] ),
            new( "Vela", [Set( "VELA" )] ),
            new( "Vercel", [Any( "NOW_BUILDER", "VERCEL" )] ),
            new( "Visual Studio App Center", [Set( "APPCENTER_BUILD_ID" )] ),
            new( "Woodpecker", [Equal( ("CI", "woodpecker") )] ),
            new( "Xcode Cloud", [Set( "CI_XCODE_PROJECT" )] ),
            new( "Xcode Server", [Set( "XCS" )] )
        ];

    /// <summary>
    /// Detects a CI run: <c>CI</c> is not the string <c>false</c>, and a known vendor matches or one of the
    /// <see cref="_genericVariables"/> is not empty.
    /// </summary>
    /// <param name="getEnvironmentVariable">Returns the value of an environment variable, null when it is not set.</param>
    /// <returns>
    /// Null when this is not a CI run. Otherwise the vendor name, as ci-info names it (the last matching vendor of the table),
    /// or, when no vendor matches, the first non-empty variable of <see cref="_genericVariables"/>.
    /// </returns>
    internal static string? Detect( Func<string, string?> getEnvironmentVariable )
    {
        if( getEnvironmentVariable( "CI" ) == "false" )
        {
            return null;
        }
        for( int i = _vendors.Length - 1; i >= 0; --i )
        {
            var vendor = _vendors[i];
            if( vendor.Conditions.All( condition => condition( getEnvironmentVariable ) ) )
            {
                return vendor.Name;
            }
        }
        return _genericVariables.FirstOrDefault( name => !string.IsNullOrEmpty( getEnvironmentVariable( name ) ) );
    }

    static Func<Func<string, string?>, bool> Set( string name )
    {
        return get => !string.IsNullOrEmpty( get( name ) );
    }

    static Func<Func<string, string?>, bool> Includes( string name, string text )
    {
        return get => get( name )?.Contains( text, StringComparison.Ordinal ) is true;
    }

    static Func<Func<string, string?>, bool> Any( params string[] names )
    {
        return get => names.Any( name => !string.IsNullOrEmpty( get( name ) ) );
    }

    static Func<Func<string, string?>, bool> Equal( params (string Name, string Value)[] values )
    {
        return get => values.All( v => get( v.Name ) == v.Value );
    }
}
