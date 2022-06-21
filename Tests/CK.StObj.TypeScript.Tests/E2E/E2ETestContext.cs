using CK.Core;
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

        E2ETestContext( NormalizedPath tsOutput, ServiceProvider services )
        {
            TypeScriptOutputPath = tsOutput;
            _services = services;
        }

        public void Dispose()
        {
            _services.Dispose();
        }

        static public E2ETestContext Create( string testName, params Type[] types ) => Create( testName, null, types );

        static public E2ETestContext Create( string testName, Action<StObjContextRoot.ServiceRegister>? configureServices, params Type[] types )
        {
            var (tsOutput,config) = LocalTestHelper.CreateTSAwareConfig( testName, null, "E2E" );
            var c = TestHelper.CreateStObjCollector( types );
            c.RegisterType( typeof( PocoJsonSerializer ) );
            var services = TestHelper.GetAutomaticServices( c, configureServices ).Services;
            return new E2ETestContext( tsOutput, services );
        }
    }
}
