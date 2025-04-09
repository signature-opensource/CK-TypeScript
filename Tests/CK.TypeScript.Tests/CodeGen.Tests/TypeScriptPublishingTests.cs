using CK.Core;
using CK.TypeScript.CodeGen;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Immutable;
using static CK.Testing.MonitorTestHelper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved

namespace CK.TypeScript.Tests;

[TestFixture]
public class TypeScriptPublishingTests
{
    [Test]
    public void path_projection_must_be_handled_carefully()
    {
        ImmutableOrdinalSortedStrings sA = new( "a.cs", "aa.cs", "a/a.cs" );
        ImmutableOrdinalSortedStrings sF = new( "#.cs", "##.cs", "#/#.cs" );
        sA.All.IsSortedStrict( StringComparer.Ordinal.Compare ).ShouldBeTrue();
        sF.All.IsSortedStrict( StringComparer.Ordinal.Compare ).ShouldBeTrue();
        /*
            These codepoints are lower than '/'.

             NUL SOH STX ETX EOT ENQ ACK BEL BS  HT LF  VT  FF CR SO SI
             DLE DC1 DC2 DC3 DC4 NAK SYN ETB CAN EM SUB ESC FS GS RS US 
             SPACE 	!	"	#	$	%	&	'	(	)	*	+	,	-	.

            => Traversing a tree with folders/files must consider this.
         */
        sA.All.ShouldBe( ["a.cs", "a/a.cs", "aa.cs"] );
        sF.All.ShouldBe( ["##.cs", "#.cs", "#/#.cs"] );
    }


    [TestCase( '#' )]
    [TestCase( 'a' )]
    public void Folder_Files_to_CodeGenContainer( char charFile )
    {
        var root = new TypeScriptRoot( libraryVersionConfiguration: ImmutableDictionary<string, CSemVer.SVersionBound>.Empty,
                                       pascalCase: false,
                                       generateDocumentation: true,
                                       ignoreVersionsBound: false );

        root.Root.FindOrCreateTypeScriptFile( $"{charFile}.ts" );
        root.Root.FindOrCreateTypeScriptFile( $"{charFile}{charFile}.ts" );
        root.Root.FindOrCreateTypeScriptFile( $"{charFile}/{charFile}.ts" );

        var codeTarget = new CodeGenResourceContainerTarget();
        root.Publish( TestHelper.Monitor, codeTarget ).ShouldBeTrue();
    }
}
