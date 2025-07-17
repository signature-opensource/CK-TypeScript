using CK.TS.Angular;

namespace CK.Ng.AspNet.Auth.UserProfilePage;

/// <summary>
/// This component has no router-outlet (<see cref="NgComponentAttribute.HasRoutes"/> is false).
/// If needed this can be transformed and routes must be manually handled (<see cref="NgRoutedComponent"/>
/// can be used - when ForceTargetHasRoutes will be implemented).
/// </summary>
[NgRoutedComponent<PrivatePageComponent>( Route = "profile" )]
public sealed class UserProfilePageComponent : NgRoutedComponent
{
}
