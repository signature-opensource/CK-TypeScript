using CK.Core;
using CK.Testing;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.IO;
using static CK.Testing.StObjEngineTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.StObj.TypeScript.Tests
{

    [TestFixture]
    public class PocoTypeScriptTests
    {
        [ExternalName( "NotGeneratedByDefault" )]
        public interface INotGeneratedByDefault : IPoco
        {
            int Power { get; set; }
        }

        /// <summary>
        /// IPoco are not automatically exported.
        /// Using the [TypeScript] attribute declares the type.
        /// </summary>
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
            TestHelper.GenerateTypeScript( targetProjectPath, TestHelper.CreateTypeCollector( typeof( INotGeneratedByDefault ) ), Type.EmptyTypes );
            File.Exists( targetProjectPath.Combine( "ck-gen/src/CK/StObj/TypeScript/Tests/NotGeneratedByDefault.ts" ) ).Should().BeFalse();
        }

        [Test]
        public void no_TypeScript_attribute_is_generated_when_referenced()
        {
            // NotGeneratedByDefault is generated because it is referenced by IGeneratedByDefault.
            var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();
            TestHelper.GenerateTypeScript( targetProjectPath, TestHelper.CreateTypeCollector( typeof( IGeneratedByDefault ), typeof( INotGeneratedByDefault ) ), Type.EmptyTypes );
            File.Exists( targetProjectPath.Combine( "ck-gen/src/CK/StObj/TypeScript/Tests/NotGeneratedByDefault.ts" ) ).Should().BeTrue();
        }

        [Test]
        public void no_TypeScript_attribute_is_generated_when_Type_appears_in_Aspect()
        {
            // NotGeneratedByDefault is generated because it is configured.
            var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();
            TestHelper.GenerateTypeScript( targetProjectPath, TestHelper.CreateTypeCollector( typeof( INotGeneratedByDefault ) ), new[] { typeof( INotGeneratedByDefault ) } );
            File.ReadAllText( targetProjectPath.Combine( "ck-gen/src/CK/StObj/TypeScript/Tests/NotGeneratedByDefault.ts" ) )
                .Should().Contain( "export class NotGeneratedByDefault" );
        }
    }
}
