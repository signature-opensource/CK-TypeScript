using CK.Setup;
using CK.Testing;
using CK.Text;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;
using static CK.Testing.StObjEngineTestHelper;

namespace CK.StObj.TypeScript.Tests
{
    [TestFixture]
    public class TypeScriptCodeGeneratorTests
    {
        static readonly NormalizedPath _outputFolder = TestHelper.TestProjectFolder.AppendPart( "TestOutput" );

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

        static NormalizedPath GenerateTSCode( string testName, params Type[] types )
        {
            var output = TestHelper.CleanupFolder( _outputFolder.AppendPart( testName ), false );
            var config = new StObjEngineConfiguration();
            config.Aspects.Add( new TypeScriptAspectConfiguration() );
            var b = new BinPathConfiguration();
            b.AspectConfigurations.Add( new XElement( "TypeScript", new XElement( "OutputPath", output ) ) );

            config.BinPaths.Add( b );

            var engine = new StObjEngine( TestHelper.Monitor, config );
            engine.Run( new MonoCollectorResolver( types ) ).Should().BeTrue( "StObjEngine.Run worked." );
            Directory.Exists( output ).Should().BeTrue();
            return output;
        }

        [TypeScript( SameFolderAs = typeof(IPocoLike) )]
        public enum Power
        {
            None,
            Medium,
            Strong
        }


        public interface IPocoLike
        {
            string Name { get; set; }

            Power Power { get; set; }

            IAnotherPocoLike Friend { get; }
        }

        public interface IAnotherPocoLike
        {
            int Age { get; set; }

            Power AnotherPower { get; set; }

            IPocoLike AnotherFriend { get; set; }
        }









    }
}
