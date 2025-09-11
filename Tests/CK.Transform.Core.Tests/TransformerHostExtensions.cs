using CK.Core;
using CK.Transform.Core;
using Shouldly;
using System;
using static CK.Testing.MonitorTestHelper;

namespace CK;

static class TransformerHostExtensions
{
    public static void ApplyAndCheck( this TransformerHost host, string source, string transformer, string result )
    {
        Throw.Assert( Environment.NewLine == "\r\n" || Environment.NewLine == "\n" );
        source.ReplaceLineEndings().ShouldBe( source );

        Run( host, source, transformer, result );
        Run( host, source, RevertLineEndings( transformer ), result );
        Run( host, RevertLineEndings( source ), transformer, result );
        Run( host, RevertLineEndings( source ), RevertLineEndings( transformer ), result );

        static void Run( TransformerHost host, string source, string transformer, string result )
        {
            var function = host.TryParseFunction( TestHelper.Monitor, transformer ).ShouldNotBeNull();
            var sourceCode = host.Transform( TestHelper.Monitor, source, function ).ShouldNotBeNull();
            sourceCode.ToString().ReplaceLineEndings().ShouldBe( result );
        }

    }


    public static string RevertLineEndings( string s )
    {
        return s.ReplaceLineEndings( Environment.NewLine == "\r\n" ? "\n" : "\r\n" );
    }
}
