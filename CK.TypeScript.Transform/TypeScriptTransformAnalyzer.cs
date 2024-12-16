using CK.Transform.Core;
using CK.Transform.TransformLanguage;

namespace CK.TypeScript.Transform;

sealed class TypeScriptTransformAnalyzer : BaseTransformAnalyzer
{
    readonly TransformerHost _host;

    internal TypeScriptTransformAnalyzer( TransformerHost host, TypeScriptLanguage language )
        : base( host, language )
    {
        _host = host;
    }

    protected override IAbstractNode? ParseStatement( ref ParserHead head )
    {
        return base.ParseStatement( ref head );
    }

}
