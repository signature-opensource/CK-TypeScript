using System.Collections.Generic;

namespace CK.Core;

using Ref = ResPackageDescriptor.Ref;

sealed partial class CoreCollector
{
    sealed class TopologicalSorter
    {
        readonly IActivityMonitor _monitor;
        readonly CoreCollector _collector;
        // The sorter never alters the index. Only the CoreCollector does.
        readonly IReadOnlyDictionary<object, ResPackageDescriptor> _packageIndex;

        public TopologicalSorter( IActivityMonitor monitor, CoreCollector collector )
        {
            _monitor = monitor;
            _collector = collector;
            _packageIndex = collector._packageIndex;
        }

        public bool Sort()
        {

        }

        bool ResolveReferences( string relName, List<Ref>? list )
        {
            if( list == null ) return true;
            bool success = true;
            for( int i = 0; i < list.Count; i++ )
            {
                var r = list[i];
                if( r.IsValid )
                {
                    r = InitialResolveRef( relName, r );
                    if( r.IsValid )
                    {
                        list[i] = r;
                    }
                    else
                    {
                        if( r.IsOptional )
                        {
                            list.RemoveAt( i-- );
                        }
                        else
                        {
                            success = false;
                        }
                    }
                }
                else
                {
                    list.RemoveAt( i-- );
                }
            }
            return success;
        }

        // The idea here is that the first "Resolve" pass calls this on each and every
        // references.
        // This can add new packages to the end of the _packages list of non
        // optional packages (this first pass must use a for, not a foreach).
        //
        // Optional references to optional packages are returned as-is.
        // Optional references to registered non-optional packages returns a non optional reference to the package.
        // Required reference to registered non-optional packages returns a non optional reference to the package.
        // Required reference to registered but optional packages promotes the package to be non-optional and returns
        // a non optional reference to the package.
        // Required reference to unexisting packages use the Typed/NamedResPackageResolvers to return a non optional package
        // or fails by returning Ref.Invalid.
        //
        // A returned Ref.Invalid signals an error.
        //
        // This supports the "automatic closure discovery" feature because an extension of the initial set of types
        // is possible thanks to the Typed/NamedResPackageResolvers but under their control: when no resolver are configured,
        // the initial set of types is the definitive one.
        //
        // After the "Resolve" pass, the packages that are still optional can be removed and the second "Sort" pass
        // can start, ignoring any optional references.
        //
        Ref InitialResolveRef( string relName, Ref r )
        {
            Throw.DebugAssert( r.IsValid );
            var p = r.AsPackageDescriptor;
            if( p != null )
            {
                if( !r.IsOptional && p.IsOptional )
                {
                    _collector.SetNoMoreOptionalPackage( p );
                }
                return p.CheckContext( _monitor, relName, _collector._packageDescriptorContext )
                        ? r
                        : default;
            }
            if( _packageIndex.TryGetValue( r._ref, out var result ) )
            {
                // The package is known. It is not optional, we are done
                // whether the reference is optional or not: it is no more optional.
                if( !result.IsOptional )
                {
                    return result;
                }
                // The package is optional, if the ref is also optional,
                // we return the optional reference unchanged.
                if( r.IsOptional )
                {
                    return r;
                }
                // This is a non optional ref to an optional package:
                // We promote the package: it is no more optional and
                // we return the resolved package.
                _collector.SetNoMoreOptionalPackage( result );
                return result;
            }
            // The package is unknown. If this is an optional
            // reference, we return the optional reference unchanged.
            if( r.IsOptional )
            {
                return r;
            }
            // The package is unknown but this is a required reference.
            // => Time for the resolvers (typed and named) to kick in
            //    (if they exist).
            return ResolveWithResolvers( r );
        }

        Ref ResolveWithResolvers( Ref r )
        {
            Throw.DebugAssert( r.IsValid );
            ResPackageDescriptor? result;
            var type = r.AsType;
            if( type != null )
            {
                if( _collector._typedPackageResolver != null )
                {
                    if( _collector._packageExcluder != null )
                    {
                        if( !_collector._packageExcluder.AllowRequired( _monitor, type ) )
                        {
                            _monitor.Error( $"Reference '{r}' is explicitly excluded by the Package excluder." );
                            return Ref.Invalid;
                        }
                    }
                    if( !_collector._typedPackageResolver.ResolveRequired( _monitor, _collector, type )
                        || !_packageIndex.TryGetValue( r._ref, out result ) )
                    {
                        _monitor.Error( $"TypedPackageResolver failed to register the reference '{r}'." );
                        return Ref.Invalid;
                    }
                    // Fix an optional registration by the TypedPackageResolver.
                    if( result.IsOptional )
                    {
                        _collector.SetNoMoreOptionalPackage( result );
                        return result;
                    }
                }
                _monitor.Error( $"No associated TypedPackageResolver. The reference '{r}' is not registered." );
                return Ref.Invalid;
            }
            var fullName = r.FullName;
            if( _collector._namedPackageResolver != null )
            {
                if( _collector._packageExcluder != null )
                {
                    if( !_collector._packageExcluder.AllowRequired( _monitor, fullName ) )
                    {
                        _monitor.Error( $"Reference '{r}' is explicitly excluded by the Package excluder." );
                        return Ref.Invalid;
                    }
                }
                if( !_collector._namedPackageResolver.ResolveRequired( _monitor, _collector, fullName )
                    || !_packageIndex.TryGetValue( r._ref, out result ) )
                {
                    _monitor.Error( $"NamedPackageResolver failed to register the reference '{r}'." );
                    return Ref.Invalid;
                }
                // Fix an optional registration by the NamedPackageResolver.
                if( result.IsOptional )
                {
                    _collector.SetNoMoreOptionalPackage( result );
                    return result;
                }
            }
            _monitor.Error( $"No associated NamedPackageResolver. The reference '{r}' is not registered." );
            return Ref.Invalid;
        }
    }
}
