using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;

namespace CK.Transform.TransformLanguage;


public sealed partial class TransformerHost
{
    /// <summary>
    /// Hosts transformation of immutable <see cref="AbstractNode"/> by tracking
    /// the changed root and handling range selection.
    /// </summary>
    public sealed class Host
    {
        LocationRoot _root;
        Language _currentLanguage;

        internal Host( Language language, AbstractNode root )
        {
            Throw.CheckNotNullArgument( root );
            _root = new LocationRoot( root, false );
            _currentLanguage = language;
        }

        /// <summary>
        /// Gets the current node. 
        /// This property tracks the transformed node.
        /// </summary>
        public AbstractNode Node => _root.Node; 

        /// <summary>
        /// Gets the location manager of the current root <see cref="Node"/>.
        /// </summary>
        public INodeLocationManager LocationManager => _root;

        /// <summary>
        /// Gets the current language.
        /// </summary>
        public Language CurrentLanguage => _currentLanguage;

        /// <summary>
        /// Gets or sets whether the root <see cref="Node"/> should be reparsed.
        /// This is automatically set to true by some visitors that plays with unparsed texts.
        /// </summary>
        public bool NeedReparse { get; set; }

        /// <summary>
        /// Unconditionally reparses the root <see cref="Node"/> with the <see cref="CurrentLanguage"/>.
        /// </summary>
        /// <param name="monitor">Required monitor.</param>
        /// <param name="newLanguage">Optional new language that replaces <see cref="CurrentLanguage"/>.</param>
        /// <returns>True on success, false on error.</returns>
        public bool Reparse( IActivityMonitor monitor, Language? newLanguage = null )
        {
            using( monitor.OpenTrace( "Parsing transformation result." ) )
            {
                if( newLanguage != null )
                {
                    monitor.Trace( $"Changing language from '{_currentLanguage.LanguageName}' to '{newLanguage.LanguageName}'." );
                }
                string text = _root.Node.ToString();
                AbstractNode? newOne = ParseTarget( monitor, text, _currentLanguage );
                if( newOne == null ) return false;
                _root = new LocationRoot( newOne, false );
                NeedReparse = false;
                return true;
            }
        }

        /// <summary>
        /// Applies a <see cref="Transformer"/> to <see cref="Node"/>.
        /// <see cref="Reparse"/> is automatically called if needed at the 
        /// end of the transformation.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="transformer">The transformer.</param>
        /// <param name="scope">An optional scope for the transformation.</param>
        /// <returns>True on success, false on error.</returns>
        public bool Apply( IActivityMonitor monitor,
                           TransfomerFunction transformer,
                           NodeScopeBuilder? scope = null )
        {
            Throw.CheckNotNullArgument( monitor );
            Throw.CheckNotNullArgument( transformer );
            if( transformer.Target != null )
            {
                NodeScopeBuilder? scopeTarget = _currentLanguage.TransformLanguage.HandleTransformerTarget( monitor, transformer.Target );
                if( scope == null ) scope = scopeTarget;
                else if( scopeTarget != null )
                {
                    scope = new NodeScopeIntersect( scope, scopeTarget );
                }
            }
            if( !RunStatements( monitor, transformer.Statements, scope ) ) return false;
            return !NeedReparse || Reparse( monitor );
        }

        bool RunStatements( IActivityMonitor monitor, NodeList<ITransformStatement> statements, NodeScopeBuilder? scope )
        {
            foreach( var t in statements )
            {
                if( !RunStatement( monitor, t, scope ) )
                {
                    monitor.Error( $"Failed to apply '{t.ToString()}'." );
                    return false;
                }
            }
            return true;
        }

        bool RunStatement( IActivityMonitor monitor, ITransformStatement t, NodeScopeBuilder? scope )
        {
            if( t is InjectIntoStatement injectInto )
            {
                return Apply( new TriviaInjectionPointVisitor( monitor, injectInto ), scope );
            }
            throw new NotSupportedException( $"Transform statement '{t.ToString()}' not supported." );
        }

        /// <summary>
        /// Visits the root node with a location-aware visitor.
        /// If the visitor alters the structure, the <see cref="Node"/> is updated.
        /// </summary>
        /// <param name="transformer">A transformer visitor.</param>
        /// <param name="scope">An optional scope for the transformation.</param>
        public bool Apply( TransformVisitor transformer, NodeScopeBuilder? scope = null )
        {
            Throw.CheckNotNullArgument( transformer );
            INodeLocationRange? filter = null;
            if( scope != null )
            {
                filter = BuildRange( transformer.Monitor, scope );
                if( filter == null ) return false;
            }
            return Visit( transformer, filter );
        }

        /// <summary>
        /// Visits the root node with a location-aware visitor.
        /// If the visitor alters the structure, the <see cref="Node"/> is updated.
        /// </summary>
        /// <param name="transformer">A visitor.</param>
        /// <param name="rangeFilter">An optional filter that restricts the visit.</param>
        /// <returns>True on success, false on error.</returns>
        public bool Visit( TransformVisitor transformer, INodeLocationRange? rangeFilter = null )
        {
            Throw.CheckNotNullArgument( transformer );
            bool success = true;
            using( transformer.Monitor.OnError( () => success = false ) )
            {
                AbstractNode? r = transformer.VisitRoot( _root, rangeFilter );
                if( r != _root.Node && success )
                {
                    _root = new LocationRoot( r, false );
                    NeedReparse |= transformer.HasUnParsedText;
                }
            }
            return success;
        }

        sealed class ScopeResolver : TransformVisitor
        {
            readonly NodeScopeBuilder _builder;
            readonly List<NodeLocationRange> _ranges;

            public ScopeResolver( IActivityMonitor monitor, NodeScopeBuilder builder )
                : base( monitor )
            {
                builder.Reset();
                _builder = builder;
                _ranges = new List<NodeLocationRange>();
            }

            public INodeLocationRange Result => NodeLocationRange.Create( _ranges, _ranges.Count, false );

            protected override bool BeforeVisitItem()
            {
                INodeLocationRange? r = _builder.Enter( VisitContext );
                if( r != null )
                {
                    _ranges.AddRange( r );
                    if( r.Last.End.Position >= VisitContext.Position + VisitContext.VisitedNode.Width )
                    {
                        return false;
                    }
                }
                return true;
            }

            protected override AbstractNode AfterVisitItem( AbstractNode visitResult )
            {
                INodeLocationRange? r = _builder.Leave( VisitContext );
                if( r != null ) _ranges.AddRange( r );
                if( VisitContext.Depth == 0 )
                {
                    r = _builder.Conclude( VisitContext );
                    if( r != null ) _ranges.AddRange( r );
                }
                return visitResult;
            }
        }

        /// <summary>
        /// Applies a <see cref="NodeScopeBuilder"/> to the current <see cref="Node"/> root.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="builder">The scope builder.</param>
        /// <param name="rangeFilter">An optional filter that restricts the visit.</param>
        /// <returns>A result range or null on error.</returns>
        public INodeLocationRange? BuildRange( IActivityMonitor monitor, NodeScopeBuilder builder, INodeLocationRange? rangeFilter = null )
        {
            Throw.CheckNotNullArgument( builder );
            bool error = false;
            using( monitor.OnError( () => error = true ) )
            {
                var s = new ScopeResolver( monitor, builder );
                s.VisitRoot( _root, rangeFilter );
                return error ? null : s.Result;
            }
        }


    }

}
