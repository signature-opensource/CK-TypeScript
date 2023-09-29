using CK.Core;
using CK.Setup;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using static CK.Testing.StObjEngineTestHelper;

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
        public void new_poco_has_default_and_readonly_properties_set()
        {
            var targetProjectPath = TestHelper.GetTypeScriptWithTestsSupportTargetProjectPath();
            TestHelper.GenerateTypeScript( targetProjectPath, typeof( IWithReadOnly ), typeof( IWithUnions ) );
            TestHelper.RunTypeScriptTest( targetProjectPath );
        }

        [ExternalName( "NotGeneratedByDefault" )]
        public interface INotGeneratedByDefault : IPoco
        {
        }

        [TypeScript]
        public interface IGeneratedByDefault : IPoco
        {
            INotGeneratedByDefault Some { get; set; }
        }

        [Test]
        public void no_TypeScript_attribute_provide_no_generation()
        {
            // NotGeneratedByDefault is not generated.
            var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();
            TestHelper.GenerateTypeScript( targetProjectPath, new[] { typeof( INotGeneratedByDefault ) }, Type.EmptyTypes );
            File.Exists( targetProjectPath.Combine( "ck-gen/src/CK/StObj/TypeScript/Tests/NotGeneratedByDefault.ts" ) ).Should().BeFalse();
        }

        [Test]
        public void no_TypeScript_attribute_is_generated_when_referenced()
        {
            // NotGeneratedByDefault is generated because it is referenced by IGeneratedByDefault.
            var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();
            TestHelper.GenerateTypeScript( targetProjectPath, new[] { typeof( IGeneratedByDefault ), typeof( INotGeneratedByDefault ) }, Type.EmptyTypes );
            File.Exists( targetProjectPath.Combine( "ck-gen/src/CK/StObj/TypeScript/Tests/NotGeneratedByDefault.ts" ) ).Should().BeTrue();
        }

        [Test]
        public void no_TypeScript_attribute_is_generated_when_Type_appears_in_Aspect()
        {
            // NotGeneratedByDefault is generated because it is configured.
            var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();
            TestHelper.GenerateTypeScript( targetProjectPath, new[] { typeof( INotGeneratedByDefault ) }, new[] { typeof( INotGeneratedByDefault ) } );
            File.Exists( targetProjectPath.Combine( "ck-gen/src/CK/StObj/TypeScript/Tests/NotGeneratedByDefault.ts" ) ).Should().BeTrue();
        }
    }
}
