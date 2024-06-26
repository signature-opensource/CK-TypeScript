using CK.Core;
using CK.Setup;
using CK.Testing;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using static CK.StObj.TypeScript.Tests.RecordTests;
using static CK.Testing.StObjEngineTestHelper;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.StObj.TypeScript.Tests
{
    [TestFixture]
    public class SystemTypesTests
    {
        [TypeScript( Folder = "" )]
        public interface IWithDateAndGuid : IPoco
        {
            DateTime D { get; set; }
            DateTimeOffset DOffset { get; set; }
            TimeSpan Span { get; set; }
            IList<Guid> Identifiers { get; }
        }

        [Test]
        public void with_date_and_guid()
        {
            var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();

            var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
            engineConfig.FirstBinPath.EnsureTypeScriptConfigurationAspect(targetProjectPath, typeof(IWithDateAndGuid));
            engineConfig.FirstBinPath.AddTypes( typeof( IWithDateAndGuid ) );
            engineConfig.RunSuccessfully();

            File.Exists( targetProjectPath.Combine( "ck-gen/src/WithDateAndGuid.ts" ) ).Should().BeTrue();
        }

    }
}
