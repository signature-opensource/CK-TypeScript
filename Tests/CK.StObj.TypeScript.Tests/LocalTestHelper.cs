using CK.Setup;
using CK.Text;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
using static CK.Testing.StObjEngineTestHelper;


namespace CK.StObj.TypeScript.Tests
{
    static class LocalTestHelper
    {
        public static readonly NormalizedPath OutputFolder = TestHelper.TestProjectFolder.AppendPart( "TestOutput" );

        public static NormalizedPath GetOutputFolder( [CallerMemberName]string? testName = null )
        {
            return TestHelper.CleanupFolder( OutputFolder.AppendPart( testName ), false );
        }

        class MonoCollectorResolver : IStObjCollectorResultResolver
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
            NormalizedPath output = GetOutputFolder( testName );
            var config = new StObjEngineConfiguration();
            config.Aspects.Add( new TypeScriptAspectConfiguration() );
            var b = new BinPathConfiguration();
            b.AspectConfigurations.Add( new XElement( "TypeScript", new XElement( "OutputPath", output ) ) );
            config.BinPaths.Add( b );

            var engine = new StObjEngine( TestHelper.Monitor, config );
            engine.Run( collectorResults ).Should().BeTrue( "StObjEngine.Run worked." );
            Directory.Exists( output ).Should().BeTrue();
            return output;
        }

    }
}
