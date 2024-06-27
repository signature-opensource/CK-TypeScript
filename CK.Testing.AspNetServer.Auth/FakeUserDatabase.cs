using System.Threading.Tasks;
using CK.Core;
using System.Collections.Generic;
using CK.Auth;
using System.Linq;
using System;

namespace CK.Testing
{
    /// <summary>
    /// Fake user database that contains "System" (1), "Albert" (3172), "Robert" (3713) and "Hubert" (3714).
    /// Only "Albert" has the "Basic" provider.
    /// "Hubert" hase the "Google" provider.
    /// <para>
    /// <see cref="AllUsers"/> is totally mutable and everything is virtual.
    /// </para>
    /// </summary>
    public class FakeUserDatabase : IUserInfoProvider
    {
        readonly List<IUserInfo> _users;
        readonly IAuthenticationTypeSystem _typeSystem;

        public FakeUserDatabase( IAuthenticationTypeSystem typeSystem )
        {
            _users = new List<IUserInfo>
            {
                // Albert is registered in Basic.
                typeSystem.UserInfo.Create( 1, "System" ),
                typeSystem.UserInfo.Create( 3712, "Albert", new[] { new StdUserSchemeInfo( "Basic", DateTime.MinValue ) } ),
                typeSystem.UserInfo.Create( 3713, "Robert" ),
                // Hubert is registered in Google.
                typeSystem.UserInfo.Create( 3714, "Hubert", new[] { new StdUserSchemeInfo( "Google", DateTime.MinValue ) } )
            };
            _typeSystem = typeSystem;
        }

        public virtual IList<IUserInfo> AllUsers => _users;

        public virtual ValueTask<IUserInfo> GetUserInfoAsync( IActivityMonitor monitor, int userId )
        {
            var u = _users.FirstOrDefault( u => u.UserId == userId ) ?? _typeSystem.UserInfo.Anonymous;
            return ValueTask.FromResult( u );
        }
    }

}
