using CK.Core;
using CK.Setup;
using CK.Testing;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using static CK.Testing.StObjEngineTestHelper;


namespace CK.StObj.TypeScript.Tests
{
    static class LocalTestHelper
    {
        public static readonly NormalizedPath OutputFolder = TestHelper.TestProjectFolder.Combine( "../TypeScriptTests/Output" );

        public static (NormalizedPath ProjectPath, NormalizedPath SourcePath) GetOutputFolder( [CallerMemberName] string? testName = null )
        {
            var path = TestHelper.CleanupFolder( OutputFolder.AppendPart( testName ), false );
            return (path, path.AppendPart( "ts" ).AppendPart( "src" ));
        }

        public static (NormalizedPath ProjectPath, NormalizedPath SourcePath) GetOutputFolder( NormalizedPath subPath, [CallerMemberName] string? testName = null )
        {
            var path = TestHelper.CleanupFolder( OutputFolder.Combine( subPath ).AppendPart( testName ).ResolveDots(), false );
            return (path, path.AppendPart( "ts" ).AppendPart( "src" ));
        }

        /// <summary>
        /// Simple, mono bin path, implementation that uses the TestHelper to collect the StObj
        /// based on an explicit list of types into which typeof( CommonPocoJsonSupport ) is added.
        /// </summary>
        public class MonoCollectorResolver : IStObjCollectorResultResolver
        {
            readonly Type[] _types;

            public MonoCollectorResolver( params Type[] types )
            {
                _types = types.Append( typeof( CommonPocoJsonSupport ) ).ToArray();
            }

            public StObjCollectorResult? GetResult( RunningBinPathGroup g )
            {
                return TestHelper.GetSuccessfulResult( TestHelper.CreateStObjCollector( _types ) );
            }
        }

        public static (NormalizedPath ProjectPath, NormalizedPath SourcePath) GenerateTSCode( string testName, params Type[] types )
            => GenerateTSCode( testName, default, null, types );

        public static (NormalizedPath ProjectPath, NormalizedPath SourcePath) GenerateTSCode( NormalizedPath subPath, string testName, params Type[] types )
            => GenerateTSCode( testName, subPath, null, types );


        public static (NormalizedPath ProjectPath, NormalizedPath SourcePath) GenerateTSCode( string testName, TypeScriptAspectConfiguration configuration, params Type[] types )
            => GenerateTSCode( testName, default, configuration, types );

        /// <summary>
        /// Only generates the TS code.
        /// </summary>
        /// <param name="testName">The test name (the name of the target folder).</param>
        /// <param name="subPath">Optional folder above the <paramref name="testName"/> folder.</param>
        /// <param name="tsConfig">Optional TS aspect configuration.</param>
        /// <param name="types">The set of types to setup (the <see cref="PocoJsonSerializer"/> is automatically added).</param>
        /// <returns>The target type script folder.</returns>
        public static (NormalizedPath ProjectPath, NormalizedPath SourcePath) GenerateTSCode( string testName,
                                                                                              NormalizedPath subPath,
                                                                                              TypeScriptAspectConfiguration? tsConfig,
                                                                                              params Type[] types )
        {
            var (projectPath, sourcePath, config) = CreateTSAwareConfig( testName, tsConfig, subPath );
            var engine = new StObjEngine( TestHelper.Monitor, config );

            var collectorResults = new MonoCollectorResolver( types );
            engine.Run( collectorResults ).Success.Should().BeTrue( "StObjEngine.Run worked." );

            Directory.Exists( sourcePath ).Should().BeTrue();

            return (projectPath, sourcePath);
        }

        public static (NormalizedPath ProjectPathOutput, NormalizedPath TypeScriptOutput, StObjEngineConfiguration Config) CreateTSAwareConfig( string testName,
                                                                                                              TypeScriptAspectConfiguration? tsConfig = null,
                                                                                                              NormalizedPath subPath = default )
        {
            var output = GetOutputFolder( subPath, testName );
            var config = new StObjEngineConfiguration();
            config.Aspects.Add( tsConfig ?? new TypeScriptAspectConfiguration() );
            var b = new BinPathConfiguration();
            b.CompileOption = CompileOption.Compile;
            b.ProjectPath = TestHelper.TestProjectFolder;
            b.AspectConfigurations.Add( new XElement( "TypeScript",
                                            new XAttribute( "OutputPath", output.ProjectPath ),
                                            new XElement( "Barrels",
                                                new XElement( "Barrel", new XAttribute( "Path", "" ) ) ) ) );
            config.BinPaths.Add( b );
            return (output.ProjectPath, output.SourcePath, config);
        }

        public static T? JsonDeserialize<T>( IServiceProvider services, ReadOnlySpan<byte> b ) where T : class, IPoco
        {
            return services.GetRequiredService<IPocoFactory<T>>().JsonDeserialize( b );
        }

        public static T? JsonDeserialize<T>( IServiceProvider services, string s ) where T : class, IPoco
        {
            return JsonDeserialize<T>( services, Encoding.UTF8.GetBytes( s ) );
        }

        public static T? JsonRoundtrip<T>( IServiceProvider services, T? o, IActivityMonitor? monitor = null ) where T : class, IPoco
        {
            byte[] bin1;
            string bin1Text;
            var directory = services.GetRequiredService<PocoDirectory>();
            using( var m = new MemoryStream() )
            {
                try
                {
                    o.JsonSerialize( m, true );
                    bin1 = m.ToArray();
                    bin1Text = Encoding.UTF8.GetString( bin1 );
                }
                catch( Exception )
                {
                    // On error, bin1 and bin1Text can be inspected here.
                    throw;
                }

                var r1 = new Utf8JsonReader( bin1 );

                var o2 = directory.JsonDeserialize( bin1 );

                m.Position = 0;
                o2.JsonSerialize( m, true );
                var bin2 = m.ToArray();

                bin1.Should().BeEquivalentTo( bin2 );

                // On success, log.
                monitor?.Debug( bin1Text );

                // Is this an actual Poco or a definer?
                // When it's a definer, there is no factory!
                var f = services.GetService<IPocoFactory<T>>();
                if( f != null )
                {
                    return f.JsonDeserialize( bin2 );
                }
                return (T?)o2;
            }

        }

    }
}
