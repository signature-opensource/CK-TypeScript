using CK.Core;
using CK.Setup;
using CK.Testing;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using static CK.Testing.StObjEngineTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.StObj.TypeScript.Tests
{
    [TestFixture]
    public class TypeScriptRunnerTests
    {
        public interface IWithUnions : IPoco
        {
            /// <summary>
            /// Gets or sets a nullable int or string.
            /// </summary>
            [UnionType]
            object? NullableIntOrString { get; set; }

            /// <summary>
            /// Gets or sets a non nullable int or string.
            /// </summary>
            [UnionType]
            object IntOrStringNoDefault { get; set; }

            /// <summary>
            /// Gets or sets a non nullable int or string with default.
            /// </summary>
            [DefaultValue( 3712 )]
            [UnionType]
            object IntOrStringWithDefault { get; set; }

            /// <summary>
            /// Gets or sets a non nullable int or string with default.
            /// </summary>
            [DefaultValue( 42 )]
            [UnionType]
            object? NullableIntOrStringWithDefault { get; set; }

            readonly struct UnionTypes
            {
                public (int, string) NullableIntOrString { get; }
                public (int, string) IntOrStringNoDefault { get; }
                public (int, string) IntOrStringWithDefault { get; }
                public (int, string) NullableIntOrStringWithDefault { get; }
            }
        }

        /// <summary>
        /// Demonstrates the read only properties support.
        /// </summary>
        public interface IWithReadOnly : IPoco
        {
            /// <summary>
            /// Gets or sets the required target path.
            /// </summary>
            [DefaultValue( "The/Default/Path" )]
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
            /// Gets the union types poco.
            /// </summary>
            IWithUnions Poco { get; }
        }

        public record struct RecordData( DateTime Time, List<string> Names );

        public interface IWithTyped : IPoco
        {
            IList<RecordData> TypedList { get; }
            IDictionary<string, IWithUnions> TypedDic1 { get; }
            IDictionary<IWithReadOnly, string> TypedDic2 { get; }
        }

        [Test]
        public async Task TypeScriptRunner_with_environment_variables_Async()
        {
            var targetProjectPath = TestHelper.GetTypeScriptWithTestsSupportTargetProjectPath();
            TestHelper.GenerateTypeScript( targetProjectPath,
                                           typeof( IWithReadOnly ),
                                           typeof( IWithUnions ),
                                           typeof( IWithTyped ) );
            await using var runner = TestHelper.CreateTypeScriptRunner( targetProjectPath,
                                                                        new Dictionary<string, string>()
                                                                        {
                                                                            { "SET_BY_THE_UNIT_TEST", "YES!" }
                                                                        } );
            await TestHelper.SuspendAsync( resume => resume );
            runner.Run();
        }

    }
}
