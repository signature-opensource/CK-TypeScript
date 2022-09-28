using CK.Core;
using CK.Setup;
using NUnit.Framework;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.StObj.TypeScript.Tests
{
    [TestFixture]
    public class JsonSerializationTests
    {

        [ExternalName("SimpleEnum")]
        [TypeScript(Folder = "")]
        public enum SimpleEnum
        {
            None,
            One,
            Two,
            Three
        }

        [ExternalName("Simple")]
        [TypeScript( Folder = "" )]
        public interface ISimple : IPoco
        {
            int Power { get; set; }

            string Name { get; set; }

            SimpleEnum Value { get; set; }
            
        }

        [Test]
        public void simple_types_serialization()
        {
            var output = LocalTestHelper.GenerateTSCode( "Json",
                                                         nameof( simple_types_serialization ),
                                                         new TypeScriptAspectConfiguration() { SkipTypeScriptBuild = true },
                                                         typeof( SimpleEnum ),
                                                         typeof( ISimple ) );
        }
    }
}
