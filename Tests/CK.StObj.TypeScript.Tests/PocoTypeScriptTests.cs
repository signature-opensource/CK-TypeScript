using CK.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.StObj.TypeScript.Tests
{
    [TestFixture]
    public class PocoTypeScriptTests
    {
        /// <summary>
        /// IPoco are not automatically exported.
        /// Using the [TypeScript] attribute declares the type.
        /// </summary>
        [TypeScript]
        public interface IWithUnions : IPoco
        {
            [UnionType]
            object? NullableIntOrString { get; set; }

            [UnionType]
            object NonNullableListOrArrayOrDouble { get; set; }

            struct UnionTypes
            {
                public (int?,string) NullableIntOrString { get; }
                public (List<string?>,IDictionary<IPoco,ISet<int?>>[],double) NonNullableListOrArrayOrDouble { get; }
            }
        }

        [Test]
        public void with_union_types()
        {
            var output = LocalTestHelper.GenerateTSCode( nameof( with_union_types ), typeof( IWithUnions ) );
        }

        [TypeScript]
        public interface IWithReadOnly : IPoco
        {
            IList<string> List { get; }
            IDictionary<string, double> Map { get; }
            ISet<string> Set { get; }
            IWithUnions Poco { get; }
        }

        [Test]
        public void array_set_maps_and_IPoco_can_be_readonly()
        {
            var output = LocalTestHelper.GenerateTSCode( nameof( array_set_maps_and_IPoco_can_be_readonly ), typeof( IWithReadOnly ), typeof( IWithUnions ) );
        }
    }
}
