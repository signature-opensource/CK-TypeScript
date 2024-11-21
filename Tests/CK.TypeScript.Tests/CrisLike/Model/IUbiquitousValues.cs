using CK.Core;

#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved

namespace CK.CrisLike;

/// <summary>
/// Defines an extensible set of properties that can be initialized from or need to
/// be challenged against the endpoint services.
/// <para>
/// The <see cref="IUbiquitousValuesCollectCommand"/> sent to the endpoint returns these values.
/// </para>
/// </summary>
public interface IUbiquitousValues : IPoco
{
}
