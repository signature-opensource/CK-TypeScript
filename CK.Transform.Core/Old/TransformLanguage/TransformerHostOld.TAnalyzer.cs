using CK.Transform.Core;

namespace CK.Transform.Core;


public sealed partial class TransformerHostOld
{
    sealed class TAnalyzer : BaseTransformAnalyzer
    {
        public TAnalyzer( TransformerHostOld host )
            : base( host, host._transformLanguage )
        {
        }

        internal IAbstractNode ParseExpectedFunction( ref ParserHead head )
        {
            var f = Parse( ref head );
            return f ?? head.CreateError( "Expecting 'create <language> transformer [name] [on <target>] [as] begin ... end'." );
        }

    }
}
