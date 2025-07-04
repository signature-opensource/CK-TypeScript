sealed class BothFilesFilter : IFileEventFilter
{
    readonly LocalPackagesFilter _f1;
    readonly CKGenAppFilter _f2;

    public BothFilesFilter( LocalPackagesFilter f1, CKGenAppFilter f2 )
    {
        _f1 = f1;
        _f2 = f2;
    }

    public object? GetChange( string path ) => _f1.GetChange( path ) ?? _f2.GetChange( path );
}
