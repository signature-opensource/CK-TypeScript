namespace CK.Core;

public interface ILiveUpdater
{
    void OnChange( IActivityMonitor monitor, string path );

    bool AllyChanges( IActivityMonitor monitor );
}
