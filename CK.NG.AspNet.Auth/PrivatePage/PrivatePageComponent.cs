using CK.Core;
using CK.TS.Angular;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Ng.AspNet.Auth;

[NgComponent( HasRoutes = true )]
[Package<TSPackage>]
//[SrcAppComponentTransform( """"
//  create /*idempotent*/ <html> transformer
//  begin
//    unless <CK.Ng.AspNetAuth>
//    begin
//      inject before * "if( isAuthenticated() ) {";
//      inject after * """
//        } @else {
//        <ck-private-page />
//        }
//        """;
//    end
//  end
//  """" )]
public sealed class PrivatePageComponent : NgComponent
{
}
