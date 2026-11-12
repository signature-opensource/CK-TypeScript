using CK.Setup;
using NUnit.Framework;
using Shouldly;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.TypeScript.Tests;

[TestFixture]
public class CKWatchScriptTests
{
    [TestCase( "../../bin/Debug/net10.0/CK.TypeScript.LiveEngine.dll", "../../bin/${BUILD_CONFIGURATION:-Debug}/net10.0/CK.TypeScript.LiveEngine.dll" )]
    [TestCase( "../../bin/Release/net10.0/CK.TypeScript.LiveEngine.dll", "../../bin/${BUILD_CONFIGURATION:-Debug}/net10.0/CK.TypeScript.LiveEngine.dll" )]
    [TestCase( "../bin/Release/bin/Debug/net10.0/X.dll", "../bin/Release/bin/${BUILD_CONFIGURATION:-Debug}/net10.0/X.dll" )]
    [TestCase( "../../bin/net10.0/CK.TypeScript.LiveEngine.dll", "../../bin/net10.0/CK.TypeScript.LiveEngine.dll" )]
    [TestCase( "../../Other/Debug/net10.0/CK.TypeScript.LiveEngine.dll", "../../Other/Debug/net10.0/CK.TypeScript.LiveEngine.dll" )]
    public void the_configuration_folder_is_the_BUILD_CONFIGURATION_variable( string relative, string expected )
    {
        TypeScriptContext.GetCKWatchPath( relative ).ShouldBe( expected );
    }
}
