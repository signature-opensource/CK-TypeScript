using CK.Setup;

namespace CK.Core;

/// <summary>
/// A reference to a package.
/// <see cref="TPackageDescriptor"/> is it own required reference and <see cref="TPackageDescriptorRef"/>
/// implements a named reference (possibly optional).
/// </summary>
public interface ITPackageDescriptorRef : IDependentItemContainerRef
{
}
