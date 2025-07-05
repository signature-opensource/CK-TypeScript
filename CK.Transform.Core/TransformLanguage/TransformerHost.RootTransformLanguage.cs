namespace CK.Transform.Core;


public sealed partial class TransformerHost
{
    internal static readonly string _transformLanguageName = "Transform";

    /// <summary>
    /// Singleton "Transform" language itself: its transform and target analyzers are the same.
    /// Its file extensions are ".transform" and ".t".
    /// </summary>
    public static TransformLanguage RootTransformLanguage = new ThisTransformLanguage();

    sealed class ThisTransformLanguage : TransformLanguage
    {
        protected internal override TransformLanguageAnalyzer CreateAnalyzer( Language language )
        {
            return new TransformLanguageAnalyzer( language );
        }
    }
}
