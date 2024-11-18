using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System;

namespace CK.EmbeddedResources.Tests;

[TestFixture]
public class ImmutableOrdinalSortedStringsTests
{
    [Test]
    public void GetPrefixedStrings_by_prefix_simple_test()
    {
        ImmutableOrdinalSortedStrings s = new( "a", "pA", "pA", "pB", "pC", "z" );
        ReadOnlyMemory<string> r = s.GetPrefixedStrings( "p" );
        r.Length.Should().Be( 4 );
        r.ToArray().Should().BeEquivalentTo( ["pA", "pA", "pB", "pC"] );

        r = s.GetPrefixedStrings( "" );
        r.ToArray().Should().BeEquivalentTo( s.All );

        r = s.GetPrefixedStrings( "\u00FF" );
        r.Length.Should().Be( 0 );

        r = s.GetPrefixedStrings( "a" );
        r.Length.Should().Be( 1 );
        r.Span[0].Should().Be( "a" );

        r = s.GetPrefixedStrings( "aa" );
        r.Length.Should().Be( 0 );

        r = s.GetPrefixedStrings( "!" );
        r.Length.Should().Be( 0 );

        r = s.GetPrefixedStrings( "zz" );
        r.Length.Should().Be( 0 );

        r = s.GetPrefixedStrings( "pA" );
        r.Length.Should().Be( 2 );
        r.Span[0].Should().Be( "pA" );
        r.Span[1].Should().Be( "pA" );
    }


    [Test]
    public void IsPrefix()
    {
        ImmutableOrdinalSortedStrings s = new( "zz", "zzz", "a", "pA", "pA", "pB", "pC" );
        s.IsPrefix( "" ).Should().BeTrue();
        s.IsPrefix( "p" ).Should().BeTrue();
        s.IsPrefix( "z" ).Should().BeTrue();

        s.IsPrefix( "zz" ).Should().BeFalse();
        s.IsPrefix( "a" ).Should().BeFalse();
        s.IsPrefix( "aa" ).Should().BeFalse();
        s.IsPrefix( "pX" ).Should().BeFalse();
        s.IsPrefix( "b" ).Should().BeFalse();
    }

    [Test]
    public void IsPrefix_is_false_if_the_string_exists()
    {
        ImmutableOrdinalSortedStrings s = new( "prefix1" );
        s.IsPrefix( "prefix" ).Should().BeTrue();
        s.IsPrefix( "prefix1" ).Should().BeFalse();

        s = new( "prefix1", "prefix2", "prefi" );
        s.IsPrefix( "prefix" ).Should().BeTrue();
        s.IsPrefix( "prefix1" ).Should().BeFalse();

    }

}
