using CK.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.StObj.TypeScript.Tests
{
    [TestFixture]
    public class SystemTypesTests
    {
        [TypeScript]
        public interface IWithDateAndGuid : IPoco
        {
            DateTime D { get; set; }
            DateTimeOffset DOffset { get; set; }
            TimeSpan Span { get; set; }
            List<Guid> Identifiers { get; }
        }

        [Test]
        public void with_date_and_guid()
        {
            var output = LocalTestHelper.GenerateTSCode( nameof( with_date_and_guid ),
                                                         typeof( IWithDateAndGuid ) );
        }



    }
}
