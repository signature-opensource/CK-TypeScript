interface IFileEventFilter
{
    object? GetChange( string path );
}
