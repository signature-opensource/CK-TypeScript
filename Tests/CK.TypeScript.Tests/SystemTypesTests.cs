using CK.Core;
using CK.Setup;
using CK.Testing;
using Shouldly;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.TypeScript.Tests;

[TestFixture]
public class SystemTypesTests
{
    [TypeScriptType( Folder = "" )]
    public interface IWithDateAndGuid : IPoco
    {
        DateTime D { get; set; }
        DateTimeOffset DOffset { get; set; }
        TimeSpan Span { get; set; }
        IList<Guid> Identifiers { get; }
    }

    [Test]
    public async Task with_date_and_guid_Async()
    {
        var targetProjectPath = TestHelper.GetTypeScriptGeneratedOnlyTargetProjectPath();

        var engineConfig = TestHelper.CreateDefaultEngineConfiguration( compileOption: CompileOption.None );
        engineConfig.FirstBinPath.EnsureTypeScriptConfigurationAspect( targetProjectPath, typeof( IWithDateAndGuid ) );
        engineConfig.FirstBinPath.Types.Add( typeof( IWithDateAndGuid ) );
        await engineConfig.RunSuccessfullyAsync();

        File.Exists( targetProjectPath.Combine( "ck-gen/WithDateAndGuid.ts" ) ).ShouldBeTrue();
    }

}
