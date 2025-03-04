namespace CK.Core;

/// <summary>
/// Named reference to a <see cref="TPackageDescriptorRef"/> that can be optional.
/// </summary>
/// <param name="FullName">The package full name.</param>
/// <param name="Optional">True to declare an optional reference.</param>
public record class TPackageDescriptorRef( string FullName, bool Optional = false ) : ITPackageDescriptorRef
{
    /// <summary>
    /// Initializes a new <see cref="TPackageDescriptorRef"/> with a <see cref="FullName"/>
    /// optionaly starting with '?'.
    /// </summary>
    /// <param name="fullName">Full name of the object. May start with '?'.</param>
    public static TPackageDescriptorRef Create( string fullName )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( fullName );
        if( fullName[0] == '?' )
        {
            return new TPackageDescriptorRef( fullName.Substring( 1 ), true );
        }
        return new TPackageDescriptorRef( fullName , false );
    }
}
