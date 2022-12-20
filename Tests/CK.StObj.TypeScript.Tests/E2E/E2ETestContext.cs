using CK.Core;
using CK.Setup;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CK.Testing.StObjEngineTestHelper;

namespace CK.StObj.TypeScript.Tests.E2E
{
    class E2ETestContext : IDisposable
    {
        public NormalizedPath TypeScriptOutputPath { get; }

        public IServiceProvider ServiceProvider => _services;

        public T CreatePoco<T>() where T : IPoco => ServiceProvider.GetRequiredService<IPocoFactory<T>>().Create();
        public T CreatePoco<T>( Action<T> config ) where T : IPoco => ServiceProvider.GetRequiredService<IPocoFactory<T>>().Create( config );

        readonly ServiceProvider _services;

        E2ETestContext( string testName, NormalizedPath tsOutput, ServiceProvider services )
        {
            TestHelper.Monitor.OpenInfo( $"*** Test: {testName}" );
            TypeScriptOutputPath = tsOutput;
            _services = services;
        }

        public void Dispose()
        {
            _services.Dispose();
            TestHelper.Monitor.CloseGroup();
        }

        static public E2ETestContext Create( string testName, params Type[] types ) => Create( testName, default, null, null, null, types );

        static public E2ETestContext Create( string testName,
                                             NormalizedPath subPath,
                                             Action<StObjContextRoot.ServiceRegister>? configureServices,
                                             TypeScriptAspectConfiguration? tsConfig,
                                             TypeScriptAspectBinPathConfiguration? tsBinPathConfig,
                                             params Type[] types )
        {
            var collector = TestHelper.CreateStObjCollector( types );
            collector.RegisterType( TestHelper.Monitor, typeof( CommonPocoJsonSupport ) );
            var outputPath = LocalTestHelper.GetOutputFolder( subPath, testName );
            var services = TestHelper.CreateAutomaticServices( collector,
                                                               engineConfiguration => ApplyTSAwareConfig( engineConfiguration, outputPath.ProjectPath, tsConfig, tsBinPathConfig ),
                                                               startupServices: null,
                                                               configureServices ).Services;
            return new E2ETestContext( testName, outputPath.ProjectPath, services );
        }

        public static StObjEngineConfiguration ApplyTSAwareConfig( StObjEngineConfiguration c,
                                                                   NormalizedPath outputPath,
                                                                   TypeScriptAspectConfiguration? tsConfig = null,
                                                                   TypeScriptAspectBinPathConfiguration? tsBinPathConfig = null )
        {
            if( tsConfig == null )
            {
                tsConfig = new TypeScriptAspectConfiguration();
            }
            if( tsBinPathConfig == null )
            {
                tsBinPathConfig = new TypeScriptAspectBinPathConfiguration
                {
                    OutputPath = outputPath
                };
            }
            c.Aspects.Add( tsConfig );
            c.BinPaths[0].AspectConfigurations.Add( tsBinPathConfig.ToXml() );
            return c;
        }


    }
}
