namespace CK.Core;

sealed class LocalFunctionsSource : FunctionsSource
{
    public LocalFunctionsSource( IResPackageResources resources, string fullResourceName, string text )
        : base( resources, fullResourceName, text )
    {
    }
}
