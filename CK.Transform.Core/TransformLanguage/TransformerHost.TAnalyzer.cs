using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.Transform.TransformLanguage;


public sealed partial class TransformerHost
{
    sealed class TAnalyzer : BaseTransformAnalyzer
    {
        readonly TransformerHost _host;

        public TAnalyzer( TransformerHost host )
            : base( host, host._transformLanguage )
        {
            _host = host;
        }

        internal IAbstractNode ParseExpectedFunction( ref ParserHead head )
        {
            var f = Parse( ref head );
            return f ?? head.CreateError( "Expecting 'create <language> transformer [name] [on <target>] as begin ... end'." );
        }

    }
}
