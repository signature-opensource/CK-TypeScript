using Microsoft.Net.Http.Headers;
using System.Net;
using System.Text.RegularExpressions;

namespace CK.Testing
{
    /// <summary>
    /// Brings useful extensions to cookie container.
    /// </summary>
    public static class CookieContainerExtensions
    {
        /// <summary>
        /// Clears cookies from a base path and optional sub paths.
        /// </summary>
        /// <param name="container">The cookie container to update.</param>
        /// <param name="basePath">The base url. Should not be null.</param>
        /// <param name="subPath">Sub paths for which cookies must be cleared.</param>
        static public void ClearCookies( this CookieContainer container, Uri basePath, IEnumerable<string> subPath )
        {
            foreach( Cookie c in container.GetCookies( basePath ) )
            {
                c.Expired = true;
            }
            if( subPath != null )
            {
                foreach( string u in subPath )
                {
                    if( string.IsNullOrWhiteSpace( u ) ) continue;
                    Uri normalized = new Uri( basePath, u[u.Length - 1] != '/' ? u + '/' : u );
                    foreach( Cookie c in container.GetCookies( normalized ) )
                    {
                        c.Expired = true;
                    }
                }
            }
        }

        /// <summary>
        /// Clears cookies from a base path and optional sub paths.
        /// </summary>
        /// <param name="container">The cookie container to update.</param>
        /// <param name="basePath">The base url. Should not be null.</param>
        /// <param name="subPath">Optional sub paths for which cookies must be cleared.</param>
        static public void ClearCookies( this CookieContainer container, Uri basePath, params string[] subPath ) => ClearCookies( container, basePath, (IEnumerable<string>)subPath );


        /// <summary>
        /// Helper that checks its parameters and returns the request uri.
        /// </summary>
        /// <param name="container">A cookie container that must not be null./</param>
        /// <param name="response">The received response.</param>
        /// <returns>The requested uri.</returns>
        public static Uri GetCheckedRequestAbsoluteUri( this CookieContainer container, HttpResponseMessage response )
        {
            if( container == null ) throw new ArgumentNullException( nameof( container ) );
            if( response == null ) throw new ArgumentNullException( nameof( response ) );
            var uri = response.RequestMessage.RequestUri;
            if( uri == null ) throw new ArgumentNullException( "response.RequestMessage.RequestUri" );
            if( !uri.IsAbsoluteUri ) throw new ArgumentException( "Uri must be absolute.", "response.RequestMessage.RequestUri" );
            return uri;
        }
    }

}
