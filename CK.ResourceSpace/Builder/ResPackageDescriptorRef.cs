namespace CK.Core;

/// <summary>
/// Named reference to a <see cref="ResPackageDescriptorRef"/> that can be optional.
/// </summary>
/// <param name="FullName">The package full name.</param>
/// <param name="Optional">True to declare an optional reference.</param>
public record class ResPackageDescriptorRef( string FullName, bool Optional = false ) : IResPackageDescriptorRef
{
    /// <summary>
    /// Initializes a new <see cref="ResPackageDescriptorRef"/> with a <see cref="FullName"/>
    /// optionaly starting with '?'.
    /// </summary>
    /// <param name="fullName">Full name of the object. May start with '?'.</param>
    public static ResPackageDescriptorRef Create( string fullName )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( fullName );
        if( fullName[0] == '?' )
        {
            return new ResPackageDescriptorRef( fullName.Substring( 1 ), true );
        }
        return new ResPackageDescriptorRef( fullName , false );
    }
}
