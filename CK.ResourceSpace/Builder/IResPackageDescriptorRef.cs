using CK.Setup;

namespace CK.Core;

/// <summary>
/// A reference to a package.
/// <see cref="ResPackageDescriptor"/> is it own required reference and <see cref="ResPackageDescriptorRef"/>
/// implements a named reference (possibly optional).
/// </summary>
public interface IResPackageDescriptorRef : IDependentItemContainerRef
{
}
