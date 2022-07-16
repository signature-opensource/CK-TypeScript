using CK.Core;
using CK.Setup;
using CK.Testing;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Buffers;
using System.Collections.Generic;
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

        public static NormalizedPath GetOutputFolder( [CallerMemberName]string? testName = null )
        {
            return TestHelper.CleanupFolder( OutputFolder.AppendPart( testName ), false );
        }

        public static NormalizedPath GetOutputFolder( NormalizedPath subPath, [CallerMemberName]string? testName = null )
        {
            return TestHelper.CleanupFolder( OutputFolder.Combine( subPath ).AppendPart( testName ).ResolveDots(), false );
        }

        /// <summary>
        /// Simple, mono bin path, implementation that uses the TestHelper to collect the StObj
        /// based on an explicit list of types into which typeof( PocoJsonSerializer ) is added.
        /// </summary>
        public class MonoCollectorResolver : IStObjCollectorResultResolver
        {
            readonly Type[] _types;

            public MonoCollectorResolver( params Type[] types )
            {
                _types = types.Append( typeof( PocoJsonSerializer ) ).ToArray();
            }

            public StObjCollectorResult? GetResult( RunningBinPathGroup g )
            {
                return TestHelper.GetSuccessfulResult( TestHelper.CreateStObjCollector( _types ) );
            }
        }

        public static NormalizedPath GenerateTSCode( string testName, params Type[] types )
        {
            return GenerateTSCode( testName, types, null, default );
        }

        public static NormalizedPath GenerateTSCode( NormalizedPath subPath, string testName, params Type[] types )
        {
            return GenerateTSCode( testName, types, null, subPath );
        }

        public static NormalizedPath GenerateTSCode( string testName, TypeScriptAspectConfiguration configuration, params Type[] types )
        {
            return GenerateTSCode( testName, types, configuration, default );
        }

        /// <summary>
        /// Only generates the TS code.
        /// </summary>
        /// <param name="testName">The test name (the name of the target folder).</param>
        /// <param name="types">The set of types to setup (the <see cref="PocoJsonSerializer"/> is automatically added).</param>
        /// <param name="tsConfig">Optional TS aspect configuration.</param>
        /// <param name="subPath">Optional folder above the <paramref name="testName"/> folder.</param>
        /// <returns>The target type script folder.</returns>
        public static NormalizedPath GenerateTSCode( string testName,
                                                     Type[] types,
                                                     TypeScriptAspectConfiguration? tsConfig,
                                                     NormalizedPath subPath )
        {
            var (output, config) = CreateTSAwareConfig( testName, tsConfig, subPath );
            var engine = new StObjEngine( TestHelper.Monitor, config );

            var collectorResults = new MonoCollectorResolver( types );
            engine.Run( collectorResults ).Success.Should().BeTrue( "StObjEngine.Run worked." );

            Directory.Exists( output ).Should().BeTrue();
            return output;
        }


        public static (NormalizedPath TypeScriptOutput, StObjEngineConfiguration Config) CreateTSAwareConfig( string testName,
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
                                            new XElement( "OutputPath", output ),
                                            new XElement( "Barrels",
                                                new XElement( "Barrel", new XAttribute( "Path", "" ) ) ) ) );
            config.BinPaths.Add( b );
            return (output, config);
        }

        /// <summary>
        /// Serializes the Poco in UTF-8 Json.
        /// </summary>
        /// <param name="o">The poco.</param>
        /// <param name="withType">True to emit types.</param>
        /// <returns>The bytes.</returns>
        public static ReadOnlyMemory<byte> JsonSerialize( IPoco o, bool withType )
        {
            var m = new ArrayBufferWriter<byte>();
            using( var w = new Utf8JsonWriter( m ) )
            {
                o.Write( w, withType );
                w.Flush();
            }
            return m.WrittenMemory;
        }

        public static T? JsonDeserialize<T>( IServiceProvider services, ReadOnlySpan<byte> b ) where T : class, IPoco
        {
            var r = new Utf8JsonReader( b );
            var f = services.GetRequiredService<IPocoFactory<T>>();
            return f.Read( ref r );
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
                    using( var w = new Utf8JsonWriter( m ) )
                    {
                        o.Write( w, true );
                        w.Flush();
                    }
                    bin1 = m.ToArray();
                    bin1Text = Encoding.UTF8.GetString( bin1 );
                }
                catch( Exception )
                {
                    // On error, bin1 and bin1Text can be inspected here.
                    throw;
                }

                var r1 = new Utf8JsonReader( bin1 );

                var o2 = directory.Read( ref r1, new PocoJsonSerializerOptions { Mode = PocoJsonSerializerMode.ECMAScriptStandard } );

                m.Position = 0;
                using( var w2 = new Utf8JsonWriter( m ) )
                {
                    o2.Write( w2, true );
                    w2.Flush();
                }
                var bin2 = m.ToArray();

                bin1.Should().BeEquivalentTo( bin2 );

                // On success, log.
                monitor?.Debug( bin1Text );

                // Is this an actual Poco or a definer?
                // When it's a definer, there is no factory!
                var f = services.GetService<IPocoFactory<T>>();
                if( f != null )
                {
                    var r2 = new Utf8JsonReader( bin2 );
                    var o3 = f.Read( ref r2 );
                    return o3;
                }
                return (T?)o2;
            }

        }

    }
}
