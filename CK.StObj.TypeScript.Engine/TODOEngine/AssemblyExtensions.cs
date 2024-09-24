using CK.Setup;
using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CK.Core;


static class AssemblyExtensions
{
    public static string? TryGetCKResourceString( this Assembly a, IActivityMonitor monitor, string resourceName )
    {
        using var s = TryOpenCKResourceStream( a, monitor, resourceName );
        return s == null
                ? null
                : new StreamReader( s, detectEncodingFromByteOrderMarks: true, leaveOpen: true ).ReadToEnd();
    }

    public static Stream? TryOpenCKResourceStream( this Assembly a, IActivityMonitor monitor, string resourceName )
    {
        if( resourceName is null
            || resourceName.Length <= 3
            || !resourceName.StartsWith( "ck@" )
            || resourceName.Contains( '\\' ) )
        {
            Throw.ArgumentException( $"Invalid resource name '{resourceName}'. It must start with \"ck@\" and must not contain '\\'." );
        }
        var s = a.GetManifestResourceStream( resourceName );
        if( s == null )
        {
            var resName = resourceName.AsSpan().Slice( 3 );
            var fName = Path.GetFileName( resName );
            string? shouldBe = null;
            var resNames = a.GetSortedResourceNames();
            foreach( string c in resNames )
            {
                if( c.StartsWith( "ck@" ) && c.AsSpan().EndsWith( fName, StringComparison.OrdinalIgnoreCase ) )
                {
                    shouldBe = c;
                    if( c[c.Length - fName.Length - 1] == '/' )
                    {
                        break;
                    }
                }
            }
            monitor.Error( $"CK resource not found: '{resName}' in assembly '{a.GetName().Name}'.{(shouldBe == null ? string.Empty : $" It seems to be '{shouldBe.AsSpan().Slice(3)}'.")}" );
        }
        return s;
    }
}
