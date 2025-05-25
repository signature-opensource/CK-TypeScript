using CK.BinarySerialization;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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

        internal bool TryFindInsertionPoint( IActivityMonitor monitor,
                                             FunctionSource source,
                                             TransformerFunction f,
                                             out TFunction? before )
        {
            int insertIndex = source.Resources.Index;
            // Start from last: initial setup follows the topology order.
            before = _lastFunction;
            while( before != null && before._source.Resources.Index > insertIndex )
            {
                before = before._prevFunction;
            }
            if( before != null && before._source.Resources == source.Resources )
            {
                monitor.Error( $"""
                    Two transformers targeting the same target cannot be defined in the same set of resources:
                    {source.Origin} defines:
                    {f.Text}

                    And {before._source.Origin} defines: 
                    {before._function.Text}

                    Both targets '{before._target.TransfomableTargetName}'.
                    """ );
                return false;   
            }
            return true;
        }

        internal void Add( TFunction f, TFunction? before )
        {
            Throw.DebugAssert( f._nextFunction == null && f._prevFunction == null );
            if( before == null )
            {
                if( _firstFunction == null )
                {
                    Throw.DebugAssert( _lastFunction == null );
                    _firstFunction = _lastFunction = f;
                }
                else
                {
                    _firstFunction._prevFunction = f;
                    f._nextFunction = _firstFunction;
                    _firstFunction = f;
                }
            }
            else
            {
                if( (f._nextFunction = before._nextFunction) == null )
                {
                    _lastFunction = f;
                }
                else
                {
                    before._nextFunction!._prevFunction = f;
                }
                before._nextFunction = f;
                f._prevFunction = before;
            }
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

        internal readonly string? Transform( IActivityMonitor monitor,
                                             TransformerHost transformerHost,
                                             ReadOnlyMemory<char> text,
                                             bool idempotenceCheck = false )
        {
            var transformers = TryGetTransfomerFunctions( monitor, transformerHost );
            if( transformers == null ) return null;
            var sourceCode = transformerHost.Transform( monitor, text, transformers, idempotenceCheck );
            return sourceCode?.ToString();
        }

        internal void Read( IBinaryDeserializer d )
        {
            _firstFunction = d.ReadNullableObject<TFunction>();
            _lastFunction = d.ReadNullableObject<TFunction>();
        }

        internal void Write( IBinarySerializer s )
        {
            s.WriteNullableObject( _firstFunction );
            s.WriteNullableObject( _lastFunction );
        }

#if DEBUG
        internal bool Contains( TFunction f )
        {
            var e = _lastFunction;
            while( e != null )
            {
                if( e == f ) return true;
                e = e._nextFunction;
            }
            return false;
        }
#endif

    }
}
