using NUnit.Framework;
using CK.Core;
using FluentAssertions;

namespace CK.Core.Tests
{
    [TestFixture]
    public class TEMPNormalizedPathTests
    {
        [TestCase( "//a/b", "//a/b", "" )]
        [TestCase( "/", "/", "" )]
        [TestCase( "/a", "/b", "../b" )]
        [TestCase( "//", "//a/b", "a/b" )]
        [TestCase( "//a/b", "//", "../.." )]
        [TestCase( "//a/b", "//a", ".." )]
        [TestCase( "c:/", "c:/a", "a" )]
        [TestCase( "c:/a", "c:/a/b", "b" )]
        [TestCase( "c:/a/b", "c:/", "../.." )]
        [TestCase( "http://a.b/c/d", "http://a.b", "../.." )]
        [TestCase( "http://a.b/c/d", "http://a.b/c", ".." )]
        [TestCase( "http://a.b/c/d", "http://a.b/c/e", "../e" )]
        [TestCase( "http://", "http://", "" )]
        public void GetRelativePath_valid_test( string source, string target, string expected )
        {
            var s = new NormalizedPath( source );
            s.TryGetRelativePathTo( target, out var relative ).Should().BeTrue();
            relative.Should().Be( new NormalizedPath( expected ) );
            s.Combine( relative ).ResolveDots().Should().Be( target );
        }

        [TestCase( "//a/b", "" )]
        [TestCase( "", "/a" )]
        [TestCase( "a", "a" )]
        [TestCase( "http://a", "http://b" )]
        public void GetRelativePath_invalid_test( string source, string target )
        {
            new NormalizedPath( source ).TryGetRelativePathTo( target, out var relative ).Should().BeFalse();
        }
    }
}
