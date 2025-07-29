namespace CK.TS.Angular;

/// <summary>
/// [TEMPORARY]Category interface for a single component.
/// <para>
/// This should be replaced by a [NgSingleComponent] (attribute targets: interface
/// or not sealed class). This attribute will specify the expected component name:
/// 
/// [NgSingleAbstractComponent( "user-info-box" )]
/// public interface INgUserInfoBoxComponent : INgComponent {}
/// 
/// </para>
/// </summary>
public interface INgSingleComponent : INgComponent 
{
}

