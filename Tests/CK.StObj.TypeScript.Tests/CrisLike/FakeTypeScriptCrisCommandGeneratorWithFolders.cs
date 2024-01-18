using CK.Core;
using CK.CrisLike;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.StObj.TypeScript.Tests.CrisLike
{
    // This static class is only here to trigger the global FakeTypeScriptCrisCommandGeneratorImplWithFolders ITSCodeGenerator.
    // This is the same as the static class TypeScriptCrisCommandGenerator in CK.Cris.TypeScript package.
    [ContextBoundDelegation( "CK.StObj.TypeScript.Tests.CrisLike.FakeTypeScriptCrisCommandGeneratorWithFoldersImpl, CK.StObj.TypeScript.Tests" )]
    public static class FakeTypeScriptCrisCommandGeneratorWithFolders
    {
    }

    // This one changes the folders.
    // The real one doesn't do this and injects code into the Poco TypeSript implementation.
    public class FakeTypeScriptCrisCommandGeneratorWithFoldersImpl : FakeTypeScriptCrisCommandGeneratorImpl
    {
        public override bool OnResolveObjectKey( IActivityMonitor monitor, TypeScriptContext context, TSTypeRequiredEventArgs e )
        {
            if( !base.OnResolveObjectKey( monitor, context, e ) ) return false;
            return true;
        }

        public override bool OnResolveType( IActivityMonitor monitor,
                                            TypeScriptContext context,
                                            TypeBuilderRequiredEventArgs builder )
        {
            if( !base.OnResolveType( monitor, context, builder ) ) return false;
            // All ICommand here (without specified TypeScript Folder) will be in Cris/Commands.
            // Their FileName will be without the "I" and prefixed by "CMD".
            // The real CommandDirectoryImpl does nothing here: ICommand are IPoco and
            // their folder/file organization is fine.
            if( typeof( IAbstractCommand ).IsAssignableFrom( builder.Type ) )
            {
                if( builder.SameFolderAs == null )
                {
                    builder.Folder ??= builder.Type.Namespace!.Replace( '.', '/' );

                    const string autoMapping = "CK/StObj/TypeScript/Tests";
                    if( builder.Folder.StartsWith( autoMapping ) )
                    {
                        builder.Folder = string.Concat( "Commands/", builder.Folder.AsSpan( autoMapping.Length ) );
                    }
                }
                if( builder.FileName == null && builder.SameFileAs == null )
                {
                    builder.FileName = string.Concat( "CMD", builder.Type.Name.AsSpan( 1 ), ".ts" );
                }
            }
            return true;
        }

    }
}
