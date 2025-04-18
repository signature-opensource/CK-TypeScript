using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using static System.Net.Mime.MediaTypeNames;

namespace CK.Core;

sealed partial class TFunction
{
    internal struct TransformableImpl
    {
        TFunction? _firstFunction;
        TFunction? _lastFunction;

        [MemberNotNullWhen(true, nameof( _firstFunction ), nameof( FirstFunction ) )]
        internal readonly bool HasFunctions => _firstFunction != null;

        internal readonly TFunction? FirstFunction => _firstFunction;

        internal readonly TFunction? LastFunction => _lastFunction;

        internal void Add( TFunction f )
        {
            Throw.DebugAssert( f._nextFunction == null && f._prevFunction == null );
            if( (f._prevFunction = _lastFunction) == null ) _lastFunction = f;
            else _lastFunction!._nextFunction = f;
            _lastFunction = f;
        }

        internal void Remove( TFunction f )
        {
            if( _firstFunction == f ) _firstFunction = f._nextFunction;
            else f._prevFunction!._nextFunction = f._nextFunction;
            if( _lastFunction == f ) _lastFunction = f._prevFunction;
            else f._nextFunction!._prevFunction = f._prevFunction;
        }

        readonly List<TransformerFunction>? TryGetTransfomerFunctions( IActivityMonitor monitor, TransformerHost transformerHost )
        {
            Throw.DebugAssert( HasFunctions );
            var result = new List<TransformerFunction>();
            var f = _firstFunction;
            do
            {
                var t = f.GetTransformerFunction( monitor, transformerHost );
                if( t == null ) return null;
                result.Add( t );
                f = f.NextFunction;
            }
            while( f != null );
            return result;
        }

        internal readonly string? Transform( IActivityMonitor monitor, TransformerHost transformerHost, string text )
        {
            var transformers = TryGetTransfomerFunctions( monitor, transformerHost );
            if( transformers == null ) return null;
            var sourceCode = transformerHost.Transform( monitor, text, transformers );
            return sourceCode?.ToString();
        }

    }
}
