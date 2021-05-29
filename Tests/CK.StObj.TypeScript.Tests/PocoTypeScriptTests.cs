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
            /// <summary>
            /// Gets or sets a nullable int or a non nullable string.
            /// </summary>
            [UnionType]
            object? NullableIntOrString { get; set; }

            /// <summary>
            /// Gets or sets a complex algebraic type.
            /// </summary>
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

        /// <summary>
        /// Demonstrates the read only properties support.
        /// </summary>
        [TypeScript]
        public interface IWithReadOnly : IPoco
        {
            /// <summary>
            /// Gets or sets the required target path.
            /// </summary>
            string TargetPath { get; set; }

            /// <summary>
            /// Gets or sets the power.
            /// </summary>
            int? Power { get; set; }

            /// <summary>
            /// Gets the mutable list of string values.
            /// </summary>
            IList<string> List { get; }

            /// <summary>
            /// Gets the mutable map from name to numeric values.
            /// </summary>
            IDictionary<string, double?> Map { get; }

            /// <summary>
            /// Gets the mutable set of unique string.
            /// </summary>
            ISet<string> Set { get; }

            /// <summary>
            /// Gets the algebraic types demonstrations.
            /// </summary>
            IWithUnions Poco { get; }
        }

        [Test]
        public void array_set_maps_and_IPoco_can_be_readonly()
        {
            var output = LocalTestHelper.GenerateTSCode( nameof( array_set_maps_and_IPoco_can_be_readonly ), typeof( IWithReadOnly ), typeof( IWithUnions ), typeof( PocoJsonSerializer ) );
        }
    }
}
