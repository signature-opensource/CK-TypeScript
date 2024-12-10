namespace CK.Transform.TransformLanguage;

internal interface INodeLocationRangeInternal : INodeLocationRange
{
    INodeLocationRangeInternal InternalSetEachNumber( int value = -1 );

    INodeLocationRangeInternal InternalSetEnd( NodeLocation end );
}
