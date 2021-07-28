using CK.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.StObj.TypeScript.Tests
{
    [TestFixture]
    public class JsonSerializartionTests
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
            var output = LocalTestHelper.GenerateTSCode( "Json", nameof( simple_types_serialization ),
                                                         typeof( SimpleEnum ),
                                                         typeof( ISimple ) );

        }
    }
}
