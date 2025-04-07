using CK.BinarySerialization;
using CK.Core;
using CK.EmbeddedResources;
using CK.TypeScript.CodeGen;
using System;
using System.Xml.Linq;

namespace CK.Setup;

public sealed partial class TypeScriptAspect
{
    static TypeScriptAspect()
    {
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
