using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;

namespace CK.Core;

public sealed partial class ResPackageDescriptor
{
    internal bool InitializeFromType( IActivityMonitor monitor, bool? isOptional )
    {
        Throw.DebugAssert( _type != null );
        _isGroup = !typeof( IResourcePackage ).IsAssignableFrom( _type );
        _isOptional = isOptional ?? _type.CustomAttributes.Any( a => a.AttributeType == typeof( OptionalTypeAttribute ) );

        bool errorSinglePackage = false;
        foreach( var a in _type.CustomAttributes )
        {
            Type aType = a.AttributeType;
            if( aType == typeof( RequiresAttribute ) )
            {
                HandleMultiName( a, ref _requires );
            }
            else if( aType == typeof( RequiredByAttribute ) )
            {
                HandleMultiName( a, ref _requiredBy );
            }
            else if( aType == typeof( GroupsAttribute ) )
            {
                HandleMultiName( a, ref _groups );
            }
            else if( aType == typeof( ChildrenAttribute ) )
            {
                HandleMultiName( a, ref _children );
            }
            else if( aType == typeof( PackageAttribute ) )
            {
                errorSinglePackage |= _package.IsValid;
                _package = (string)a.ConstructorArguments[0].Value!;
            }
            else if( aType.IsGenericType )
            {
                if( aType == typeof( PackageAttribute<> ) )
                {
                    errorSinglePackage |= _package.IsValid;
                    _package = aType.GetGenericArguments()[0];
                }
                else if( aType == typeof( RequiresAttribute<> ) || aType == typeof( RequiresAttribute<,> ) || aType == typeof( RequiresAttribute<,,> )
                         || aType == typeof( RequiresAttribute<,,,> ) || aType == typeof( RequiresAttribute<,,,,> ) || aType == typeof( RequiresAttribute<,,,,,> ) )
                {
                    AddTypes( aType.GetGenericArguments(), false, ref _requires );
                }
                else if( aType == typeof( OptionalRequiresAttribute<> ) || aType == typeof( OptionalRequiresAttribute<,> ) || aType == typeof( OptionalRequiresAttribute<,,> )
                         || aType == typeof( OptionalRequiresAttribute<,,,> ) || aType == typeof( OptionalRequiresAttribute<,,,,> ) || aType == typeof( OptionalRequiresAttribute<,,,,,> ) )
                {
                    AddTypes( aType.GetGenericArguments(), true, ref _requires );
                }
                else if( aType == typeof( RequiredByAttribute<> ) || aType == typeof( RequiredByAttribute<,> ) || aType == typeof( RequiredByAttribute<,,> )
                         || aType == typeof( RequiredByAttribute<,,,> ) || aType == typeof( RequiredByAttribute<,,,,> ) || aType == typeof( RequiredByAttribute<,,,,,> ) )
                {
                    AddTypes( aType.GetGenericArguments(), false, ref _requiredBy );
                }
                else if( aType == typeof( OptionalRequiredByAttribute<> ) || aType == typeof( OptionalRequiredByAttribute<,> ) || aType == typeof( OptionalRequiredByAttribute<,,> )
                         || aType == typeof( OptionalRequiredByAttribute<,,,> ) || aType == typeof( OptionalRequiredByAttribute<,,,,> ) || aType == typeof( OptionalRequiredByAttribute<,,,,,> ) )
                {
                    AddTypes( aType.GetGenericArguments(), true, ref _requiredBy );
                }
                else if( aType == typeof( GroupsAttribute<> ) || aType == typeof( GroupsAttribute<,> ) || aType == typeof( GroupsAttribute<,,> )
                         || aType == typeof( GroupsAttribute<,,,> ) || aType == typeof( GroupsAttribute<,,,,> ) || aType == typeof( GroupsAttribute<,,,,,> ) )
                {
                    AddTypes( aType.GetGenericArguments(), false, ref _groups );
                }
                else if( aType == typeof( OptionalGroupsAttribute<> ) || aType == typeof( OptionalGroupsAttribute<,> ) || aType == typeof( OptionalGroupsAttribute<,,> )
                         || aType == typeof( OptionalGroupsAttribute<,,,> ) || aType == typeof( OptionalGroupsAttribute<,,,,> ) || aType == typeof( OptionalGroupsAttribute<,,,,,> ) )
                {
                    AddTypes( aType.GetGenericArguments(), true, ref _groups );
                }
                else if( aType == typeof( ChildrenAttribute<> ) || aType == typeof( ChildrenAttribute<,> ) || aType == typeof( ChildrenAttribute<,,> )
                         || aType == typeof( ChildrenAttribute<,,,> ) || aType == typeof( ChildrenAttribute<,,,,> ) || aType == typeof( ChildrenAttribute<,,,,,> ) )
                {
                    AddTypes( aType.GetGenericArguments(), false, ref _children );
                }
                else if( aType == typeof( OptionalChildrenAttribute<> ) || aType == typeof( OptionalChildrenAttribute<,> ) || aType == typeof( OptionalChildrenAttribute<,,> )
                         || aType == typeof( OptionalChildrenAttribute<,,,> ) || aType == typeof( OptionalChildrenAttribute<,,,,> ) || aType == typeof( OptionalChildrenAttribute<,,,,,> ) )
                {
                    AddTypes( aType.GetGenericArguments(), true, ref _children );
                }
            }
        }
        if( errorSinglePackage )
        {
            monitor.Error( $"At most one of [Package<Type>] or [Package( \"FullName\" )] can decorate type '{_type:N}'." );
            return false;
        }
        return true;

        static void HandleMultiName( CustomAttributeData a, ref List<Ref>? list )
        {
            var commaSeparatedPackageFullnames = (string[])a.ConstructorArguments[0].Value!;
            foreach( var n in commaSeparatedPackageFullnames )
            {
                foreach( var name in n.Split( ',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries ) )
                {
                    list ??= new List<Ref>( commaSeparatedPackageFullnames.Length );
                    list.Add( name );
                }
            }
        }

        static void AddTypes( Type[] genArgs, bool optional, ref List<Ref>? list )
        {
            foreach( var t in genArgs )
            {
                list ??= new List<Ref>( genArgs.Length );
                list.Add( new Ref( t, optional ) );
            }
        }

    }


    void HandleMultiType( IActivityMonitor monitor,
                          IEnumerable<(Type GenType, Type[] GenArgs, bool Optional)> genAttributes,
                          ref List<Ref>? list,
                          params Type[] genTypes )
    {
        foreach( var genAttribute in genAttributes.Where( a => genTypes.Contains( a.GenType ) ) )
        {
            foreach( var t in genAttribute.GenArgs )
            {
                list ??= new List<Ref>( genAttribute.GenArgs.Length );
                list.Add( new Ref( t, genAttribute.Optional ) );
            }
        }
    }

}
