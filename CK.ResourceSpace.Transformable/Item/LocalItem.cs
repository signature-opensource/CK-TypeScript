namespace CK.Core;

sealed class LocalItem : TransformableItem
{
    public LocalItem( IResPackageResources resources,
                      string fullResourceName,
                      string text,
                      int languageIndex,
                      NormalizedPath targetPath )
        : base( resources, fullResourceName, languageIndex, text, targetPath )
    {
    }
}
