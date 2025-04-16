using CK.Core;

namespace Demo.PublicSection;

[EmbeddedResourceType]
[Children<Public.Footer.Package,Public.TopBar.Package>]
public class Package : IResourcePackage
{
}
