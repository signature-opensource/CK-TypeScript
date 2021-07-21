using CK.Core;
using CK.Setup;
using CK.Testing;
using CK.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using static CK.Testing.StObjEngineTestHelper;


namespace CK.StObj.TypeScript.Tests
{
    static class LocalTestHelper
    {
        public static readonly NormalizedPath OutputFolder = TestHelper.TestProjectFolder.AppendPart( "TypeScriptOutput" );

        public static NormalizedPath GetOutputFolder( [CallerMemberName]string? testName = null )
        {
            return TestHelper.CleanupFolder( OutputFolder.AppendPart( testName ), false );
        }

        /// <summary>
        /// Simple, mono bin path, implementation that uses the TestHelper to collect the StObj
        /// based on an explicit list of types.
        /// </summary>
        public class MonoCollectorResolver : IStObjCollectorResultResolver
        {
            readonly Type[] _types;

            public MonoCollectorResolver( params Type[] types )
            {
                _types = types;
            }

            public StObjCollectorResult GetUnifiedResult( BinPathConfiguration unified )
            {
                return TestHelper.GetSuccessfulResult( TestHelper.CreateStObjCollector( _types ) );
            }

            public StObjCollectorResult GetSecondaryResult( BinPathConfiguration head, IEnumerable<BinPathConfiguration> all )
            {
                throw new NotImplementedException( "There is only one BinPath: only the unified one is required." );
            }
        }

        public static NormalizedPath GenerateTSCode( string testName, params Type[] types )
        {
            return GenerateTSCode( testName, new MonoCollectorResolver( types ) );
        }

        public static NormalizedPath GenerateTSCode( string testName, IStObjCollectorResultResolver collectorResults )
        {
            return GenerateTSCode( testName, new TypeScriptAspectConfiguration(), collectorResults );
        }

        public static NormalizedPath GenerateTSCode( string testName, TypeScriptAspectConfiguration tsConfig, IStObjCollectorResultResolver collectorResults )
        {
            NormalizedPath output = GetOutputFolder( testName );
            var config = new StObjEngineConfiguration();
            config.Aspects.Add( tsConfig );
            var b = new BinPathConfiguration();
            b.AspectConfigurations.Add( new XElement( "TypeScript", new XElement( "OutputPath", output ) ) );
            config.BinPaths.Add( b );

            var engine = new StObjEngine( TestHelper.Monitor, config );
            engine.Run( collectorResults ).Should().BeTrue( "StObjEngine.Run worked." );
            Directory.Exists( output ).Should().BeTrue();
            return output;
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
            var directory = services.GetService<PocoDirectory>();
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
