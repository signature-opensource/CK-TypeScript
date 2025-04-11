using CK.BinarySerialization;
using CK.EmbeddedResources;
using System;

namespace CK.Core;

public sealed partial class ResSpace
{
    // Handles ResourceContainerWrapper, ResourceLocator and ResourceFolder binary serialization
    // that are not implemented in CK.EmbeddedResources assembly to limit it only to simple serialization.
    //
    // The ResourceContainerWrapper needs to write its inner container. We use her a surrogate approach: the
    // CodeGenResourceContainer or EmptyResourceContainer is written as a CodeGenResourceContainer (only these
    // ones are supported).
    //
    // ResourceLocator and ResourceFolder are simple drivers.
    //

    sealed class ResourceLocatorSerializer : StaticValueTypeSerializer<ResourceLocator>
    {
        public override string DriverName => "ResourceLocator";

        public override int SerializationVersion => 0;

        public static void Write( IBinarySerializer s, in ResourceLocator o )
        {
            s.WriteNullableObject( o.Container );
            s.Writer.WriteNullableString( o.FullResourceName );
        }
    }

    sealed class ResourceLocatorDeserializer : ValueTypeDeserializer<ResourceLocator>
    {
        protected override ResourceLocator ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo )
        {
            return ResourceLocator.UnsafeCreate( d.ReadNullableObject<IResourceContainer>()!, d.Reader.ReadNullableString()! );
        }
    }

    sealed class ResourceFolderSerializer : StaticValueTypeSerializer<ResourceFolder>
    {
        public override string DriverName => "ResourceFolder";

        public override int SerializationVersion => 0;

        public static void Write( IBinarySerializer s, in ResourceFolder o )
        {
            s.WriteNullableObject( o.Container );
            s.Writer.WriteNullableString( o.FullFolderName );
        }
    }

    sealed class ResourceFolderDeserializer : ValueTypeDeserializer<ResourceFolder>
    {
        protected override ResourceFolder ReadInstance( IBinaryDeserializer d, ITypeReadInfo readInfo )
        {
            return ResourceFolder.UnsafeCreate( d.ReadNullableObject<IResourceContainer>()!, d.Reader.ReadNullableString()! );
        }
    }

    static ResSpace()
    {
        BinarySerializer.DefaultSharedContext.AddSerializationDriver( typeof( ResourceLocator ), new ResourceLocatorSerializer() );
        BinaryDeserializer.DefaultSharedContext.AddDeserializerDriver( new ResourceLocatorDeserializer() );

        BinarySerializer.DefaultSharedContext.AddSerializationDriver( typeof( ResourceFolder ), new ResourceFolderSerializer() );
        BinaryDeserializer.DefaultSharedContext.AddDeserializerDriver( new ResourceFolderDeserializer() );

        // This should be something like:
        // 
        // BinarySerializer.SurrogateResolver.Default.Register<ResourceContainerWrapper,IResourceContainer>( w => w.InnerContainer ) );
        //
        // Unfortunately, true surrogates are not supported.
        //
        BinarySerializer.DefaultSharedContext.AddSerializationDriver( typeof( ResourceContainerWrapper ), new AsInnerCodeContainer() );
    }

    sealed class AsInnerCodeContainer : ReferenceTypeSerializer<ResourceContainerWrapper>, ISerializationDriverTypeRewriter
    {
        public override string DriverName => "VersionedBinarySerializable";

        public override int SerializationVersion => 0;

        public Type GetTypeToWrite( Type type ) => typeof( CodeGenResourceContainer );

        protected override void Write( IBinarySerializer s, in ResourceContainerWrapper o )
        {
            if( o.InnerContainer is CodeGenResourceContainer code )
            {
                code.WriteData( s.Writer );
            }
            else if( o.InnerContainer is EmptyResourceContainer empty )
            {
                s.Writer.Write( empty.DisplayName );
                s.Writer.Write( 0 );
            }
            else
            {
                Throw.NotSupportedException(
                    "Due to binary serialization limitation, ResourceContainerWrapper can only wrap " +
                    "a CodeGenResourceContainer or a EmptyResourceContainer." );
            }
        }
    }
}
