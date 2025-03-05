using CK.Core;
using CK.EmbeddedResources;
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
    LocaleCultureSet?[] _locales; 

    public TSLocalesResourceHandler( ResourceSpaceData spaceData )
        : base( spaceData, "ts-locales" )
    {
        _liveState = new LiveStateBuilder( this );
        _locales = new LocaleCultureSet[spaceData.Packages.Length];
    }

    protected override bool Initialize( IActivityMonitor monitor, ResourceSpaceData spaceData )
    {

    }
}
