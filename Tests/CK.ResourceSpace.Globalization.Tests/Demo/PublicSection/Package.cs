using CK.Core;
using CK.EmbeddedResources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.PublicSection;

[EmbeddedResourceType]
[Children<Public.Footer.Package,Public.TopBar.Package>]
public class Package : IResourcePackage
{
}
