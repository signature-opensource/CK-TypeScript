using CK.Core;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.TypeScript.Engine;

sealed partial class TSLocalesResourceHandler : ResourceSpaceFolderHandler
{
    // Not readonly!
    LiveStateBuilder _liveState;


    public TSLocalesResourceHandler()
        : base( "ts-locales" )
    {
        _liveState = new LiveStateBuilder( this );
    }

    protected override bool Initialize( IActivityMonitor monitor, ImmutableArray<ResPackage> packages )
    {
        throw new NotImplementedException();
    }
}
