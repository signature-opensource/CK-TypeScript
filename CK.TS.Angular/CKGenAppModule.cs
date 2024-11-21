namespace CK.TS.Angular;

/// <summary>
/// Root type that triggers the Angular engine.
/// This is considered a <see cref="NgRoutedComponent"/>.
/// </summary>
[CK.Setup.ContextBoundDelegation( "CK.TS.Angular.Engine.AngularCodeGeneratorImpl, CK.TS.Angular.Engine" )]
public static class CKGenAppModule
{
}
