using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using CK.StObj.TypeScript.Tests.CrisLike;
using CK.Core;
using System.ComponentModel;
using CK.CrisLike;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.StObj.TypeScript.Tests.E2E
{
    [TestFixture]
    public class E2ETests
    {
        // ICommand and their result are automatically declared for TypeScript generation
        // by the CommandDirectory and its CommandDirectoryImpl in Cris.
        // Since we don't fake it here, we need to declare the TypeScript support explicitly.
        [TypeScript]
        //[ExternalName("Pong")]
        public interface IPong : IPoco
        {
            [DefaultValue("I'm a pong.")]
            string Desctiption { get; set; }
        }

        [TypeScript]
        //[ExternalName("PingCommand")]
        public interface IPingCommand : ICommand<object>
        {
            object Pong { get; set; }

            struct UnionTypes
            {
                public (int, string, IPong) Pong { get; }
            }
        }

        [Test]
        public void ping_pong()
        {
            using var ctx = E2ETestContext.Create( nameof(ping_pong), typeof(IPingCommand), typeof(IPong) );
            var withString = ctx.CreatePoco<IPingCommand>( p => p.Pong = "A string." );
            var withInt = ctx.CreatePoco<IPingCommand>( p => p.Pong = 3712 );
            var withPong = ctx.CreatePoco<IPingCommand>( p => p.Pong = ctx.CreatePoco<IPong>() );

            string sString = withString.ToString()!;
            string sInt = withInt.ToString()!;
            string sPong = withPong.ToString()!;

        }
    }
}
