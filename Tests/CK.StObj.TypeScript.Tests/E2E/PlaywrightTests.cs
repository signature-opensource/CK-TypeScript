using FluentAssertions;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System.Diagnostics;
using System.Threading.Tasks;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.StObj.TypeScript.Tests.E2E
{
    [TestFixture]
    public class PlaywrightTests
    {
        [Test]
        public async Task simple_playwright_test_Async()
        {
            try
            {
                await TestAsync();
            }
            catch( Microsoft.Playwright.PlaywrightException ex ) when (ex.Message.Contains( "Please run the following command" ) )
            {
                var a = typeof( Microsoft.Playwright.Playwright ).Assembly;
                var p = a.GetType( "Microsoft.Playwright.Program" );
                Debug.Assert( p != null );
                var m = p.GetMethod( "Main", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public );
                Debug.Assert( m != null );
                m.Invoke( null, new object[] { new[] { "install" } } );

                await TestAsync();
            }

            async Task TestAsync()
            {
                using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync();
            }
        }
    }
}
