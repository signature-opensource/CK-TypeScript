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
            /// Gets or sets a complex algebraic type.
            /// </summary>
            [UnionType]
            object NonNullableListOrDictionaryOrDouble { get; set; }

            [DefaultValue( 3712 )]
            int WithDefaultValue { get; set; }

            readonly struct UnionTypes
            {
                public (int, string)? NullableIntOrString { get; }
                public (List<string?>, Dictionary<IPoco, ISet<int?>>[], double) NonNullableListOrDictionaryOrDouble { get; }
            }
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
            [DefaultValue( "The/Default/Path" )]
            string TargetPath { get; set; }

            /// <summary>
            /// Gets or sets the power.
            /// </summary>
            int? Power { get; set; }

            /// <summary>
            /// Gets the mutable list of string values.
            /// </summary>
            List<string> List { get; }

            /// <summary>
            /// Gets the mutable map from name to numeric values.
            /// </summary>
            Dictionary<string, double?> Map { get; }

            /// <summary>
            /// Gets the mutable set of unique string.
            /// </summary>
            HashSet<string> Set { get; }

            /// <summary>
            /// Gets the algebraic types demonstrations.
            /// </summary>
            IWithUnions Poco { get; }
        }

        [Test]
        public async Task TypeScriptRunner_with_environment_variables_Async()
        {
            var targetProjectPath = TestHelper.GetTypeScriptWithTestsSupportTargetProjectPath();
            TestHelper.GenerateTypeScript( targetProjectPath, typeof( IWithReadOnly ), typeof( IWithUnions ) );
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
