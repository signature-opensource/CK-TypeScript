
namespace CK.Core;

sealed partial class LocalFunctionSource : FunctionSource, ILocalInput
{
    ILocalInput? _prev;
    ILocalInput? _next;

    public LocalFunctionSource( IResPackageResources resources, string fullResourceName, string text )
        : base( resources, fullResourceName, text )
    {
    }

    public string FullPath => _fullResourceName;

    ILocalInput? ILocalInput.Prev { get => _prev; set => _prev = value; }

    ILocalInput? ILocalInput.Next { get => _next; set => _next = value; }

    public void ApplyChanges( IActivityMonitor monitor, TransformEnvironment environment )
    {
        throw new System.NotImplementedException();
    }

    public void OnChange( IActivityMonitor monitor, TransformEnvironment environment )
    {
        throw new System.NotImplementedException();
    }
}
